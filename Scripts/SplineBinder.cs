#if HAS_SPLINES
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

[AddComponentMenu("VFX/Property Binders/VFX Path")]
[VFXBinder("VFX Path")]
public class SplineBinder : VFXBinderBase
{
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

    [SerializeField]
    private SplineContainer _splineContainer;

    [SerializeField]
    [Min(2)]
    private int _pointCount = 100;

    private Texture2D _positionMapTexture;
    private Texture2D _rotationMapTexture;
    private int _currentPointCount;
    private Color[] _positions;
    private Color[] _rotations;
    private Spline _currentSpline;
    private bool _needsUpdate;
    private Vector3 _boundsCenter;
    private Vector3 _boundsSize;

    private string _currentPointCountPropertyName;
    private string _currentPositionMapPropertyName;
    private string _currentRotationMapPropertyName;
    private string _currentBoundsCenterPropertyName;
    private string _currentBoundsSizePropertyName;

    public override void Reset()
    {
        base.Reset();

        _splineContainer = GetComponent<SplineContainer>();
    }

    public override bool IsValid(VisualEffect component)
    {
        return _splineContainer?.Spline != null && _pointCount > 1
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

        if(component.HasTexture(_rotationMapPropertyName))
            component.SetTexture(_rotationMapPropertyName, _rotationMapTexture);

        if(component.HasTexture(_boundsCenterPropertyName))
            component.SetVector3(_boundsCenterPropertyName, _boundsCenter);

        if(component.HasTexture(_boundsSizePropertyName))
            component.SetVector3(_boundsSizePropertyName, _boundsSize);
    }

    private bool UpdateData()
    {
        EnsureCorrectSplineIsReferenced();
        EnsurePropertyNamesAreUpToDate();
        EnsureTexturesAreInitedAndCorrectlySized();

        if (_needsUpdate == false)
            return false;

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

            for (int i = 0; i < _pointCount; i++)
            {
                var t = i * step;
                _currentSpline.Evaluate(t, out float3 position, out float3 tangent, out float3 up);
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
                tangent = math.normalize(tangent);
                var q = quaternion.LookRotation(tangent, up);
                _positions[i] = new Color(position.x, position.y, position.z, 1);
                _rotations[i] = new Color(q.value.x, q.value.y, q.value.z, q.value.w);

                bounds.Encapsulate(position);
            }

            for (int i = 0; i < _pointCount - 1; i++)
                Debug.DrawLine(ColorToVector(_positions[i]), ColorToVector(_positions[i + 1]));

            _boundsCenter = bounds.center;
            _boundsSize = bounds.size;
        }

        _positionMapTexture.SetPixels(_positions);
        _rotationMapTexture.SetPixels(_rotations);

        _positionMapTexture.Apply();
        _rotationMapTexture.Apply();

        _needsUpdate = false;

        return true;
    }

    private Vector3 ColorToVector(Color c)
    {
        return new Vector3(c.r, c.g, c.b);
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

        _currentPointCount = _pointCount;

        _positions = new Color[width * height];
        _rotations = new Color[width * height];

        _needsUpdate = true;
    }

    private void EnsureCorrectSplineIsReferenced()
    {
        var spline = _splineContainer?.Spline;

        if (spline == _currentSpline)
            return;

        if (_currentSpline != null)
            _currentSpline.changed -= _spline_ContentsChanged;

        if (spline != null)
            spline.changed += _spline_ContentsChanged;

        _currentSpline = spline;

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

    private void _spline_ContentsChanged()
    {
        _needsUpdate = true;
    }
}
#endif