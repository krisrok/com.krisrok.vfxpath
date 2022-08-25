﻿using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace VFXPath
{
    public abstract class VFXPathBinderBase : VFXBinderBase
    {
        [SerializeField]
        [Min(2)]
        protected int _pointCount = 100;

        [VFXPropertyBinding("System.Int32"), SerializeField]
        protected ExposedProperty _pointCountPropertyName = "PointCount";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedProperty _positionMapPropertyName = "PositionMap";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedProperty _rotationMapPropertyName = "RotationMap";

        [VFXPropertyBinding("UnityEngine.Vector3"), SerializeField]
        protected ExposedProperty _boundsCenterPropertyName = "BoundsCenter";

        [VFXPropertyBinding("UnityEngine.Vector3"), SerializeField]
        protected ExposedProperty _boundsSizePropertyName = "BoundsSize";

        private string _currentPointCountPropertyName;
        private string _currentPositionMapPropertyName;
        private string _currentRotationMapPropertyName;
        private string _currentBoundsCenterPropertyName;
        private string _currentBoundsSizePropertyName;

        private Texture2D _positionMapTexture;
        private Texture2D _rotationMapTexture;
        private int _currentPointCount;

        protected bool _needsUpdate;

        protected Color[] _positions;
        protected Color[] _rotations;
        protected Vector3 _boundsCenter;
        protected Vector3 _boundsSize;

        protected abstract void FillPositionsRotationsAndBounds();

        protected abstract void EnsureReferences();

        public override bool IsValid(VisualEffect component)
        {
            return _pointCount > 1
                && component.HasTexture(_positionMapPropertyName)
                //&& component.HasTexture(m_RotationMap)
                && component.HasInt(_pointCountPropertyName);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            if (UpdateData() == false)
                return;

            component.SetInt(_pointCountPropertyName, _pointCount);
            component.SetTexture(_positionMapPropertyName, _positionMapTexture);

            if (component.HasTexture(_rotationMapPropertyName))
                component.SetTexture(_rotationMapPropertyName, _rotationMapTexture);

            if (component.HasVector3(_boundsCenterPropertyName))
                component.SetVector3(_boundsCenterPropertyName, _boundsCenter);

            if (component.HasVector3(_boundsSizePropertyName))
                component.SetVector3(_boundsSizePropertyName, _boundsSize);
        }

        private bool UpdateData()
        {
            EnsureReferences();
            EnsurePropertyNamesAreUpToDate();
            EnsureTexturesAreInitedAndCorrectlySized();

            if (_needsUpdate == false)
                return false;

            FillPositionsRotationsAndBounds();

            _positionMapTexture.SetPixels(_positions);
            _rotationMapTexture.SetPixels(_rotations);

            _positionMapTexture.Apply();
            _rotationMapTexture.Apply();

            _needsUpdate = false;

            return true;
        }

        private void EnsureTexturesAreInitedAndCorrectlySized()
        {
            if (_positionMapTexture != null
                && _positions != null
                && _pointCount == _currentPointCount)
                return;

            if (_positionMapTexture != null)
                DestroyImmediate(_positionMapTexture);
            if (_rotationMapTexture != null)
                DestroyImmediate(_rotationMapTexture);

            // Has power of two any real advantage anymore? Or could we just use a 1-dimensional texture like VFXHierarchyAttributeMapBinder ? 
            var sideLength = Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(_pointCount)));
            var width = sideLength;
            var height = sideLength;

            _positionMapTexture = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
            _rotationMapTexture = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);

            _positionMapTexture.filterMode = _rotationMapTexture.filterMode = FilterMode.Point;

            _currentPointCount = _pointCount;

            _positions = new Color[width * height];
            _rotations = new Color[width * height];

            _needsUpdate = true;
        }

        private void EnsurePropertyNamesAreUpToDate()
        {
            if (_currentPointCountPropertyName == (string)_pointCountPropertyName
                && _currentPositionMapPropertyName == (string)_positionMapPropertyName
                && _currentRotationMapPropertyName == (string)_rotationMapPropertyName
                && _currentBoundsCenterPropertyName == (string)_boundsCenterPropertyName
                && _currentBoundsSizePropertyName == (string)_boundsSizePropertyName)
                return;

            _currentPointCountPropertyName = (string)_pointCountPropertyName;
            _currentPositionMapPropertyName = (string)_positionMapPropertyName;
            _currentRotationMapPropertyName = (string)_rotationMapPropertyName;
            _currentBoundsCenterPropertyName = (string)_boundsCenterPropertyName;
            _currentBoundsSizePropertyName = (string)_boundsSizePropertyName;

            _needsUpdate = true;
        }
    }
}