#if HAS_PATHCREATOR
using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace VFXPath
{
    [AddComponentMenu("VFX/Property Binders/VFX Path/PathCreator")]
    [VFXBinder("VFX Path/PathCreator")]
    public class PathCreatorVFXPathBinder : VFXPathBinderBase
    {
        [SerializeField]
        private PathCreator _pathCreator;

        private VertexPath _currentVertexPath;

        public override void Reset()
        {
            base.Reset();

            _pathCreator = GetComponent<PathCreator>();
        }

        public override bool IsValid(VisualEffect component)
        {
            return base.IsValid(component)
                && _pathCreator?.path != null;
        }

        protected override void FillPositionsRotationsAndBounds()
        {
            if (_currentVertexPath == null)
            {
                Array.Fill(_positions, Color.black);
                Array.Fill(_rotations, Color.black);

                _boundsSize = _boundsCenter = Vector3.zero;
            }
            else
            {
                var step = _currentVertexPath.length / (_pointCount - 1);

                var bounds = new Bounds();

                var m = transform.worldToLocalMatrix;
                var r = transform.rotation;

                float distance = 0;
                for (int i = 0; i < _pointCount; i++)
                {
                    var position = m.MultiplyPoint3x4(_currentVertexPath.GetPointAtDistance(distance, EndOfPathInstruction.Extrapolate));
                    var rotation = r * _currentVertexPath.GetRotationAtDistance(distance, EndOfPathInstruction.Extrapolate);

                    _positions[i] = new Color(position.x, position.y, position.z, 1);
                    _rotations[i] = new Color(rotation.x, rotation.y, rotation.z, rotation.w);

                    if (i == 0)
                        bounds = new Bounds(position, Vector3.zero);
                    else
                        bounds.Encapsulate(position);

                    distance += step;
                }

                _boundsCenter = bounds.center;
                _boundsSize = bounds.size;
            }
        }

        protected override void EnsureReferences()
        {
            var vertexPath = _pathCreator?.path;

            if (vertexPath == _currentVertexPath)
                return;

            if (_currentVertexPath != null)
                _pathCreator.pathUpdated -= _pathCreator_ContentsChanged;

            if (vertexPath != null)
            {
                _pathCreator.pathUpdated += _pathCreator_ContentsChanged;

                if (_pathCreator.transform != transform)
                {
                    // We expect the spline to be sitting on this same transform. Warn the user if it isn't.
                    Debug.LogWarning($"The referenced {nameof(PathCreator)} should be on the same {nameof(GameObject)} as the {nameof(VisualEffect)} but is on {_pathCreator.gameObject.name} instead. " +
                        $"Auto-updating the path will not work correctly. Particles might end up in unexpected positions.", this);
                }
            }

            _currentVertexPath = vertexPath;

            _needsUpdate = true;
        }

        private void _pathCreator_ContentsChanged()
        {
            _needsUpdate = true;
        }

        public override string ToString()
        {
            return $"VFX Path: PathCreator -> {_positionMapPropertyName} {_rotationMapPropertyName}";
        }
    }
}
#endif