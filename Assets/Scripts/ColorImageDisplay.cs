using UnityEngine;
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


    // Speeds to change the HSV color
    private float _hSpeed = 0.5f;
    private float _sSpeed = 1f;
    private float _vSpeed = 1f;

    private Vector2 _startDrag;
    private Vector2 _currentDrag;

    private Image _backgroundImage;
    private Image _innerCircleImage;
    private Image _outerCircleImage;

    private float _joystickMaxMagnitude;
    
    void Start()
    {
        _backgroundImage = _background.GetComponent<Image>();
        _innerCircleImage = _innerCircle.GetComponent<Image>();
        _outerCircleImage = _outerCircle.GetComponent<Image>();

        _backgroundImage.color = Color.HSVToRGB(0.5f, 1.0f, 0.7f);

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

    void SetSpeeds(float hSpeed, float sSpeed, float vSpeed)
    {
        this._hSpeed = hSpeed;
        this._sSpeed = sSpeed;
        this._vSpeed = vSpeed;
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            // Are we clicking in the bottom 20%?
            if(Input.mousePosition.y < Screen.height * 0.1f)
            {
                _configPanel.SetActive(true);
            }
            else
            {
                _startDrag = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.z));
                _innerCircle.position = _startDrag;
                _outerCircle.position = _startDrag;
                _innerCircleImage.enabled = true;
                _outerCircleImage.enabled = true;
                _configPanel.SetActive(false);
            }
        }

        if(Input.GetMouseButton(0) && _innerCircleImage.enabled)
        {
            _currentDrag = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.z));
            Vector2 offset = _currentDrag - _startDrag;
            Vector2 direction = Vector2.ClampMagnitude(offset, _joystickMaxMagnitude);

            _innerCircle.position = new Vector2(_startDrag.x + direction.x, _startDrag.y + direction.y);
            SetSpeeds(direction.x, direction.y, 0);
        }
        else
        {
            SetSpeeds(0, 0, 0);
            _innerCircleImage.enabled = false;
            _outerCircleImage.enabled = false;
        }

        UpdateColor();
    }
    
    private float ComputeJoystickMaxMagnitude()
    {
        // This works but feels a bit magical. It seems related to the Canvas Scaler component on the Canvas
        // If I change the Canvas Scaler to be "Constant Pixel Size" instead of "Scale with Screen Size" then I don't need to multiply by the aspect ratio!
        // Also note that we're matching either the width or height but we're actually matching completely the width
        RectTransform objectRectTransform = this.GetComponentInParent<RectTransform> ();
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

}
