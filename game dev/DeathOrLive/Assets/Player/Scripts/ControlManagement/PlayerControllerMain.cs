using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerMain : MonoBehaviour
{
    [SerializeField] float _walkSpeed = 5f;
    [SerializeField] float _runSpeed = 10f;
    [SerializeField] float _jumpForce = 5f;
    float _currentSpeed;
    InputSystemManagement _playerController;
    Vector2 _moveInput;


    CharacterController _controller;

    private void Awake()
    {
        _playerController = new();

    }

    // Start is called before the first frame update
    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        _playerController.Movement.Enable();
        _playerController.Movement.Walk.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _playerController.Movement.Walk.canceled += ctx => _moveInput = Vector2.zero;
    }

    private void OnDisable()
    {
        _playerController.Movement.Walk.performed -= ctx => _moveInput = ctx.ReadValue<Vector2>();
        _playerController.Movement.Walk.canceled -= ctx => _moveInput = Vector2.zero;
        _playerController.Movement.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        Moving();
    }

    void Moving()
    {
        Vector3 _moveDir = new Vector3(_moveInput.x, 0, _moveInput.y);
        _currentSpeed = _moveInput.magnitude < 0.1f ? 0f : _walkSpeed;
        _controller.Move(_moveDir * _currentSpeed * Time.deltaTime);
    }
}
