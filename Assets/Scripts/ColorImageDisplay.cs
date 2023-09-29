using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorImageDisplay : MonoBehaviour
{
    [SerializeField]
    private GameObject _background;
    [SerializeField]
    private GameObject _configPanel;
    [SerializeField]
    private Transform _innerCircle;
    [SerializeField]
    private Transform _outerCircle;

    [SerializeField]
    private int _mouseTapMaxMs = 300;

    // Speeds to change the HSV color
    // TODO: Make this depend on a factor that can be serialized in Unity
    private float _hSpeed = 0.5f;
    private float _sSpeed = 1f;
    private float _vSpeed = 1f;

    private Vector2 _startDrag;
    private Vector2 _currentDrag;

    private Image _backgroundImage;
    private Image _innerCircleImage;
    private Image _outerCircleImage;

    private float _joystickMaxMagnitude;
    private bool _wasMousePressed;
    private bool _isFlashing;

    private System.Diagnostics.Stopwatch _mouseDownWatch = new System.Diagnostics.Stopwatch();

    private enum ColorComponent
    {
        Hue,
        Saturation,
        Brightness
    }

    private ColorComponent _horizontalColorComponent = ColorComponent.Hue;
    private ColorComponent _verticalColorComponent = ColorComponent.Saturation;
    
    void Start()
    {
        _backgroundImage = _background.GetComponent<Image>();
        _innerCircleImage = _innerCircle.GetComponent<Image>();
        _outerCircleImage = _outerCircle.GetComponent<Image>();

        Debug.Assert(_backgroundImage != null, "Game object must have an image [_backgroundImage=null]");
        Debug.Assert(_innerCircleImage != null, "Inner circle must have an image [_innerCircleImage=null]");
        Debug.Assert(_outerCircleImage != null, "Outer circle must have an image [_outerCircleImage=null]");
    }

    private void OnRectTransformDimensionsChange()
    {
        _joystickMaxMagnitude = ComputeJoystickMaxMagnitude();
    }

    // Modulus where x can be negative
    private float MathMod(float x, float m)
    {
        return x < 0 ? ((x % m) + m) % m : x % m; 
    }

    void SetSpeeds(Vector2 direction)
    {
        float hSpeed = 0;
        float sSpeed = 0;
        float vSpeed = 0;
        switch(_horizontalColorComponent)
        {
            case ColorComponent.Hue:
                hSpeed = direction.x;
                break;
            case ColorComponent.Saturation:
                sSpeed = direction.x;
                break;
            case ColorComponent.Brightness:
                vSpeed = direction.x;
                break;
            default:
                throw new NotImplementedException($"Unexpected color component value [_horizontalColorComponent={_horizontalColorComponent}");
        }
        // If both horizontal and vertical are set to the same color component the vertical will override the horizontal. We won't handle this case more elegantly for now.
        switch(_verticalColorComponent)
        {
            case ColorComponent.Hue:
                hSpeed = direction.y;
                break;
            case ColorComponent.Saturation:
                sSpeed = direction.y;
                break;
            case ColorComponent.Brightness:
                vSpeed = direction.y;
                break;
            default:
                throw new NotImplementedException($"Unexpected color component value [_verticalColorComponent={_verticalColorComponent}");
        }

        SetSpeeds(hSpeed, sSpeed, vSpeed);
    }

    void SetSpeeds(float hSpeed, float sSpeed, float vSpeed)
    {
        this._hSpeed = hSpeed;
        this._sSpeed = sSpeed;
        this._vSpeed = vSpeed;
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0) && !_isFlashing)
        {
            // IMPORTANT: All canvas objects that are not on the config panel must have the Image --> Raycast Target turned OFF so that they don't return true of IsPointerOverGameObject()
            if(EventSystem.current.IsPointerOverGameObject()  // Panel is shown, clicking on panel
                || Input.mousePosition.y < Screen.height * 0.1f) // Panel not yet shown, clicking on lower 10% of background
            {
                _configPanel.SetActive(true);
            }
            else
            {
                // Config panel was active, close the config panel and don't consider this a real press on the background
                if(_configPanel.activeSelf)
                {
                    _configPanel.SetActive(false);
                }
                else
                {
                    _wasMousePressed = true;
                    _mouseDownWatch.Start();
                }
            }
        }

        if(Input.GetMouseButton(0))
        {
            if(!_isFlashing && _mouseDownWatch.ElapsedMilliseconds >= _mouseTapMaxMs)
            {
                if(!_innerCircleImage.gameObject.activeSelf)
                {
                    AddVirtualJoystick();
                }
                else
                {
                    UpdateVirtualJoystick();
                }
            }
        }
        else if(_wasMousePressed)
        {
            _wasMousePressed = false;
            if(_innerCircleImage.gameObject.activeSelf)
            {
                RemoveVirtualJoystick();
            }
            else
            {
                FlashBackground();
            }
            _mouseDownWatch.Reset();
        }
        else
        {
            SetSpeeds(0, 0, 0);
        }

        UpdateColor();
    }

    private void AddVirtualJoystick()
    {
        _startDrag = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.z));
        _innerCircle.position = _startDrag;
        _outerCircle.position = _startDrag;
        _innerCircleImage.gameObject.SetActive(true);
        _outerCircleImage.gameObject.SetActive(true);
    }

    private void UpdateVirtualJoystick()
    {
        _currentDrag = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.z));
        Vector2 offset = _currentDrag - _startDrag;
        Vector2 direction = Vector2.ClampMagnitude(offset, _joystickMaxMagnitude);

        _innerCircle.position = new Vector2(_startDrag.x + direction.x, _startDrag.y + direction.y);
        SetSpeeds(direction);
    }

    private void RemoveVirtualJoystick()
    {
        SetSpeeds(0, 0, 0);
        _innerCircleImage.gameObject.SetActive(false);
        _outerCircleImage.gameObject.SetActive(false);
    }
    
    private float ComputeJoystickMaxMagnitude()
    {
        // This works but feels a bit magical. It seems related to the Canvas Scaler component on the Canvas
        // If I change the Canvas Scaler to be "Constant Pixel Size" instead of "Scale with Screen Size" then I don't need to multiply by the aspect ratio!
        // Also note that we're matching either the width or height but we're actually matching completely the width
        RectTransform objectRectTransform = this.GetComponentInParent<RectTransform> ();
        if(objectRectTransform == null)
        {
            // Prevent null ref when closing the app
            return 0;
        }
        float aspectRatio = objectRectTransform.rect.width / objectRectTransform.rect.height;
        float imageSizeFactor = 0.006f * _outerCircle.GetComponent<RectTransform>().rect.width; // No clue why 0.006f but it works!
        return imageSizeFactor * aspectRatio;
    }

    private void UpdateColor()
    {
        Color.RGBToHSV(_backgroundImage.color, out float h, out float s, out float v);
        // Hue should warp from 1 back to 0 and from 0 back to 1
        h += _hSpeed * Time.deltaTime;
        h = MathMod(h, 1);

        // Saturation and brightness shouldn't warp but should be clamped
        s += _sSpeed * Time.deltaTime;
        s = Mathf.Clamp(s, 0, 1);

        v += _vSpeed * Time.deltaTime;
        v = Mathf.Clamp(v, 0, 1);
        _backgroundImage.color = Color.HSVToRGB(h, s, v);
    }

    private void FlashBackground()
    {
        _isFlashing = true;
        Color previousBackgroundColor = _backgroundImage.color;
        _backgroundImage.color = new Color(1, 1, 1);
        StartCoroutine("EndFlash", previousBackgroundColor);
    }

    private IEnumerator EndFlash(Color previousBackgroundColor)
    {
        yield return new WaitForSeconds(0.1f);
        _isFlashing = false;
        _backgroundImage.color = previousBackgroundColor;
    }

    public void HorizontalAxisValueChanged(int value)
    {
        _horizontalColorComponent = GetAxisColorComponent(value);
    }

    public void VerticalAxisValueChanged(int value)
    {
        _verticalColorComponent = GetAxisColorComponent(value);
    }

    private ColorComponent GetAxisColorComponent(int value)
    {
        Debug.Assert(value >= 0 && value <= 2, $"Invalid value, should be between 0 and 2 inclusive [value={value}]");
        return value switch
        {
            0 => ColorComponent.Hue,
            1 => ColorComponent.Saturation,
            2 => ColorComponent.Brightness,
            _ => throw new NotImplementedException(),
        };
    }
}
