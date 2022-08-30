#if HAS_SPLINES
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace VFXPath
{
    [AddComponentMenu("VFX/Property Binders/VFX Path/Spline")]
    [VFXBinder("VFX Path/Spline")]
    public class SplineVFXPathBinder : VFXPathBinderBase
    {
        [SerializeField]
        private SplineContainer _splineContainer;

        private Spline _currentSpline;

        public override void Reset()
        {
            base.Reset();

            _splineContainer = GetComponent<SplineContainer>();
        }

        public override bool IsValid(VisualEffect component)
        {
            return base.IsValid(component)
                && _splineContainer?.Spline != null;
        }

        protected override void FillPositionsRotationsAndBounds()
        {
            if (_currentSpline == null)
            {
                _boundsSize = _boundsCenter = Vector3.zero;
            }
            else
            {
                var step = 1f / (_pointCount - 1);

                var bounds = new Bounds();

                float t = 0;
                for (int i = 0; i < _pointCount; i++)
                {
                    _currentSpline.Evaluate(t, out float3 position, out float3 tangent, out float3 up);

                    tangent = FixNullTangentIfNeeded(tangent, position, i, step);
                    tangent = math.normalize(tangent);
                    var rotation = quaternion.LookRotation(tangent, up).value;

                    _positions[i] = new half4((half3)position, half.zero);
                    _rotations[i] = (half4)rotation;

                    if (i == 0)
                        bounds = new Bounds(position, Vector3.zero);
                    else
                        bounds.Encapsulate(position);

                    t += step;
                }

                _boundsCenter = bounds.center;
                _boundsSize = bounds.size;
            }
        }

        private float3 FixNullTangentIfNeeded(float3 tangent, float3 position, int i, float step)
        {
            if (tangent.x == 0 && tangent.y == 0 && tangent.z == 0)
            {
                if (i < _pointCount - 1)
                {
                    var nextPosition = _currentSpline.EvaluatePosition((i + 1) * step);
                    tangent = nextPosition - position;
                }
                else
                {
                    var prevPosition = _currentSpline.EvaluatePosition((i - 1) * step);
                    tangent = position - prevPosition;
                }
            }

            return tangent;
        }

        protected override void EnsureReferences()
        {
            var spline = _splineContainer?.Spline;

            if (spline == _currentSpline)
                return;

            if (_currentSpline != null)
                _currentSpline.changed -= _spline_ContentsChanged;

            if (spline != null)
            {
                spline.changed += _spline_ContentsChanged;

                if (_splineContainer.transform != transform)
                {
                    // We expect the spline to be sitting on this same transform. Warn the user if it isn't.
                    Debug.LogWarning($"The referenced {nameof(SplineContainer)} should be on the same {nameof(GameObject)} as the {nameof(VisualEffect)} but is on {_splineContainer.gameObject.name} instead. " +
                        $"Auto-updating the path will not work correctly. Particles might end up in unexpected positions.", this);
                }
            }

            _currentSpline = spline;

            _needsUpdate = true;
        }

        private void _spline_ContentsChanged()
        {
            _needsUpdate = true;
        }

        public override string ToString()
        {
            return $"VFX Path: Spline -> {_positionMapPropertyName} {_rotationMapPropertyName}";
        }
    }
}
#endif