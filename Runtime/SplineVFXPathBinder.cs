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
                && _splineContainer != null && _splineContainer.Splines?.Count > 0;
        }

        protected override void FillPositionsRotationsAndBounds()
        {
            if (_currentSpline == null)
            {
                _boundsSize = _boundsCenter = Vector3.zero;
            }
            else
            {
                var splines = _splineContainer.Splines;
                var splinesCount = splines.Count;

                var bounds = new Bounds();

                var step = (float)splinesCount / (_pointCount - 1);

                float t = 0;
                for (int i = 0; i < _pointCount; i++)
                {
                    int splineIndex = (int)t;
                    splines[Mathf.Min(splineIndex, splinesCount - 1)].Evaluate(t - splineIndex, out float3 position, out float3 tangent, out float3 up);

                    tangent = FixNullTangentIfNeeded(tangent, position, i, step);
                    tangent = math.normalize(tangent);
                    var rotation = quaternion.LookRotation(tangent, up).value;

                    _positions[i] = new half4((half3)position, (half)splineIndex);
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
            Spline spline = null;
            if (_splineContainer != null)
                spline = _splineContainer.Spline;

            if (spline == _currentSpline)
                return;

#if !HAS_SPLINES_2_0_0
            if (_currentSpline != null)
                _currentSpline.changed -= _spline_ContentsChanged;
#else
            Spline.Changed -= _spline_AnyChanged;
#endif

            if (spline != null)
            {
#if !HAS_SPLINES_2_0_0
                spline.changed += _spline_ContentsChanged;
#else
                Spline.Changed += _spline_AnyChanged;
#endif

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

#if !HAS_SPLINES_2_0_0
        private void _spline_ContentsChanged()
        {
            _needsUpdate = true;
        }
#else
        private void _spline_AnyChanged(Spline spline, int knotIndex, SplineModification modification)
        {
            if (spline != _currentSpline)
                return;

            _needsUpdate = true;
        }
#endif

        public override string ToString()
        {
            return $"VFX Path: Spline -> {_positionMapPropertyName} {_rotationMapPropertyName}";
        }
    }
}
#endif