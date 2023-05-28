using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Xml.Schema;
using Unity.VisualScripting;
using UnityEngine;

public class CharMovement : MonoBehaviour
{
    public float yMax = 0f;
    public float yMin = 0f;
    public float xMin = 0f;
    public float xMax = 0f;
    public float yVelocity = 0f;
    public float xVelocity = 0f;

    public float xClampMin = 0f;//could be local. but good for debuging
    public float xClampMax = 0f;

    public bool enablePerspective = true;

    private float xStart = 0f;
    private float yStart = 0f;

    private Animator animator = null;
    private Animator rHandAnimator = null;
    private List<Animator> commonAnimators = null;

    private Vector3 scale = new Vector3(0.6f, 0.6f, 1f);
    private Vector2 perspective = new Vector2(0f, 0f);
    private Vector2 dir = new Vector2(0f, 0f);
    // Start is called before the first frame update
    void Start()
    {
        if (xMin >= xMax || yMin >= yMax)
            Debug.Log($"Check constraints {xMin} {xMax} {yMin} {yMax}");
        animator = GetComponent<Animator>();
        if (animator == null){
            Debug.Log("Animator component is required");            
        }
        rHandAnimator = transform.GetChild(0).gameObject.GetComponent<Animator>();
        if (rHandAnimator == null){
            Debug.Log("rHandAnimator Animator component is required");            
        }

        commonAnimators = new List<Animator>() {animator, rHandAnimator};

        xStart = transform.position.x;
        yStart = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        float velCoeff = enablePerspective ? (transform.position.y - perspective.y) / (yStart - perspective.y) : 1f;
        float xVelocityCur = xVelocity * velCoeff;
        float yVelocityCur = yVelocity * velCoeff;

        if (xVelocityCur < 0f) { scale.x = -Math.Abs(scale.x); }
        else
            if (xVelocityCur > 0f) { scale.x = Math.Abs(scale.x); }
        transform.localScale = scale * velCoeff;

        if (horizontal != 0f || vertical != 0f) {
            float positionDeltaX = horizontal * xVelocityCur;
            float positionDeltaY = vertical * yVelocityCur;
            if (enablePerspective)
            {
                dir.x = perspective.x - transform.position.x;
                dir.y = perspective.y - transform.position.y;
                if ((vertical < 0f && transform.position.y != yMin) || (vertical > 0f && transform.position.y != yMax))
                {
                    dir.Normalize();
                    positionDeltaX += positionDeltaY * dir.x;
                    positionDeltaY *= dir.y;
                }
            }
            xClampMin = xMin;
            xClampMax = xMax;
            if (enablePerspective)
            {
                float perspCoeff = (transform.position.y - yMin) / (perspective.y - yMin);
                xClampMin = xMin + (perspective.x - xMin) * perspCoeff;
                xClampMax = xMax + (perspective.x - xMax) * perspCoeff;
            }
            float x = Math.Clamp(transform.position.x + positionDeltaX, xClampMin, xClampMax);
            float y = Math.Clamp(transform.position.y + positionDeltaY, yMin, yMax);
            transform.position = new Vector3(x, y, transform.position.z);
            foreach(var a in commonAnimators){
                a.SetBool("IsWalking", true);
            }
        }
        else {
            foreach(var a in commonAnimators){
                a.SetBool("IsWalking", false);
            }
        }
        if (Input.GetKeyDown(KeyCode.LeftControl)) {
            foreach(var a in commonAnimators){
                a.SetTrigger("Roll");
            }
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            rHandAnimator.SetTrigger("Punch");
        }
    }
}
