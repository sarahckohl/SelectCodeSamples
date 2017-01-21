﻿using UnityEngine;
using System.Collections;

public class CharacterMovement : MonoBehaviour {
    

    public GameObject head;
    public GameObject eyes;

    [Header("Movement Settings")]
    public float slowMovementSpeed = 4.0f;
    public float fastMovementSpeed = 8.0f;
    public bool movementTypeToggle = false;
    public float mouseSensitive = 2.0f;
    public float upDownRange = 60.0f;

    [Header ("Jumping Settings")]
    public float crouchTime = 1.0f;
    public float minJumpSpeed = 3.0f;
    public float maxJumpSpeed = 4.0f;
    public AnimationCurve crouchCurve;
    public float uncrouchRate = 1.0f;

    [Header ("Landing Friction")]
    [Tooltip("In meters/second per second")]
    public float decelerateRate = 1;
    public AnimationCurve decelerationCurve;

    //Looking variables
    float verticalRotation = 0;
    private CharacterController cc;

    //Logic for framerate-independant deceleration
    private bool _wasGrounded = false;
    private Vector3 _startDecelerationVelocity = Vector3.zero;
    private float _startDecelerationTime = 0.0f;
    private float _endDecelerationTime = 0.0f;

    //Jump variables
    private float _crouchPercent = 0.0f;
    private Vector3 jumpVelocity = Vector3.zero;

    

    private static CharacterMovement _instance;
    public static CharacterMovement instance {
        get {
            return _instance;
        }
    }

    void Awake() {
        _instance = this;
    }

    public float chosenSpeed() {
        return (movementTypeToggle ? fastMovementSpeed : slowMovementSpeed);
    }
    // Use this for initialization
    void Start() {
        cc = GetComponent<CharacterController>();
        cc.detectCollisions = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    //5 right button
    //6 left trigger
    //7 right trigger

    // Update is called once per frame
    void Update() {
        if (Input.GetButtonDown("ToggleSpeed")) {
            movementTypeToggle = !movementTypeToggle;
        }

        //If we are paused, no control given
        if (Time.timeScale == 0f) {
            return;
        }

        float rotLeftRight = Input.GetAxis("Mouse X") * mouseSensitive;
        transform.Rotate(0, rotLeftRight, 0);

        if (upDownRange != 0) {
            verticalRotation -= Input.GetAxis("Mouse Y") * mouseSensitive;
            verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);
            eyes.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
        }

        //Generate walking velocity based on input keys
        Vector3 walkingVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * chosenSpeed();
        walkingVelocity = Quaternion.Euler(0, eyes.transform.eulerAngles.y, 0) * walkingVelocity;

        head.transform.localPosition = new Vector3(0, -crouchCurve.Evaluate(_crouchPercent), 0);

        if (cc.isGrounded) {
            if (!_wasGrounded) {
                _startDecelerationVelocity = jumpVelocity;
                float horizontalVelocity = new Vector2(jumpVelocity.x, jumpVelocity.z).magnitude;
                float decelerationTime = horizontalVelocity / decelerateRate;

                _startDecelerationTime = Time.time;
                _endDecelerationTime = _startDecelerationTime + decelerationTime;
            }

            if (Time.time < _endDecelerationTime) {
                jumpVelocity = Vector3.Lerp(_startDecelerationVelocity, Vector3.zero, decelerationCurve.Evaluate(Mathf.InverseLerp(_startDecelerationTime, _endDecelerationTime, Time.time)));
            } else {
                jumpVelocity = Vector3.zero;
            }

            jumpVelocity.y = 0;
        }
        _wasGrounded = cc.isGrounded;

        if (Input.GetButton("Jump") && cc.isGrounded) {
            _crouchPercent = Mathf.MoveTowards(_crouchPercent, 1.0f, Time.deltaTime / crouchTime);
        } else {
            _crouchPercent = Mathf.MoveTowards(_crouchPercent, 0.0f, Time.deltaTime * uncrouchRate);
        }

        if (Input.GetButtonUp("Jump") && cc.isGrounded && _crouchPercent > 0.0f) {
            float jumpMagnitude = Mathf.Lerp(minJumpSpeed, maxJumpSpeed, _crouchPercent);

            if (eyes.transform.eulerAngles.x > 345 || eyes.transform.eulerAngles.x < 15) {
                jumpVelocity = eyes.transform.forward;
                jumpVelocity.y = 1f;
                jumpVelocity = jumpVelocity.normalized * jumpMagnitude;
            } else {
                jumpVelocity = eyes.transform.forward * jumpMagnitude;
            }

            _wasGrounded = false;
        }

        jumpVelocity += Physics.gravity * Time.deltaTime;
        cc.Move((jumpVelocity + walkingVelocity) * Time.deltaTime);  
    }


}