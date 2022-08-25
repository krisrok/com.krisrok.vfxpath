#if HAS_SPLINES
using System;
using System.Collections;
using System.Collections.Generic;
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
                Array.Fill(_positions, Color.black);
                Array.Fill(_rotations, Color.black);

                _boundsSize = _boundsCenter = Vector3.zero;
            }
            else
            {
                var step = 1f / (_pointCount - 1);

                var bounds = new Bounds();

                var m = Matrix4x4.identity;
                var mr = Quaternion.identity;
                if (_splineContainer.transform != transform)
                {
                    m = transform.worldToLocalMatrix * _splineContainer.transform.localToWorldMatrix;
                    mr = transform.rotation * _splineContainer.transform.rotation;
                }

                float t = 0;
                for (int i = 0; i < _pointCount; i++)
                {
                    _currentSpline.Evaluate(t, out float3 position, out float3 tangent, out float3 up);

                    tangent = FixNullTangentIfNeeded(tangent, position, i, step);
                    tangent = math.normalize(tangent);
                    var rotation = Quaternion.LookRotation(tangent, up);

                    position = m.MultiplyPoint3x4(position);
                    rotation = mr * rotation;

                    _positions[i] = new Color(position.x, position.y, position.z, 1);
                    _rotations[i] = new Color(rotation.x, rotation.y, rotation.z, rotation.w);

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
            {
                _needsUpdate = _needsUpdate || _splineContainer.transform != transform;
                return;
            }

            if (_currentSpline != null)
                _currentSpline.changed -= _spline_ContentsChanged;

            if (spline != null)
                spline.changed += _spline_ContentsChanged;

            _currentSpline = spline;

            _needsUpdate = true;
        }

        private void _spline_ContentsChanged()
        {
            _needsUpdate = true;
        }
    }
}
#endif