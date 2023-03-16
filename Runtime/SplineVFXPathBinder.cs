#if HAS_SPLINES
using System.Collections.Generic;
using System.Linq;
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

        private IReadOnlyCollection<Spline> _currentSplines;

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
            if (_currentSplines == null)
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
                    int splineIndex = splinesCount > 1 ? (int)t : 0;
                    var spline = splines[Mathf.Min(splineIndex, splinesCount - 1)];
                    var splineT = t - splineIndex;
                    spline.Evaluate(splineT, out float3 position, out float3 tangent, out float3 up);

                    if (tangent.x == 0 && tangent.y == 0 && tangent.z == 0)
                        tangent = FixNullTangent(position, splineT, step, spline);
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

        private float3 FixNullTangent(float3 position, float t, float step, Spline spline)
        {
            if (t + step <= 1)
            {
                var nextPosition = spline.EvaluatePosition(t + step);
                return nextPosition - position;
            }
            else
            {
                var prevPosition = spline.EvaluatePosition(t - step);
                return position - prevPosition;
            }
        }

        protected override void EnsureReferences()
        {
            var splines = _splineContainer == null ? null : _splineContainer.Splines;

            if(_currentSplines == null)
            {
                if (splines == null)
                    return;
            }
            else
            {
                if (_currentSplines.SequenceEqual(splines))
                    return;
            }

#if !HAS_SPLINES_2_0_0
            if (_currentSplines != null)
            {
                foreach(var spline in _currentSplines)
                    spline.changed -= _spline_ContentsChanged;
            }
#else
            Spline.Changed -= _spline_AnyChanged;
#endif

            if (splines != null)
            {
#if !HAS_SPLINES_2_0_0
                foreach (var spline in splines)
                    spline.changed -= _spline_ContentsChanged;
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

            _currentSplines = splines;

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
            if (_currentSplines == null || _currentSplines.Contains(spline) == false)
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