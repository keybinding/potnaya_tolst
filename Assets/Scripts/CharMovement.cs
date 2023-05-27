using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharMovement : MonoBehaviour
{
    public float yMax = 0f;
    public float yMin = 0f;
    public float xMin = 0f;
    public float xMax = 0f;
    public float yVelocity = 0f;
    public float xVelocity = 0f;

    private Animator animator = null;
    private Animator rHandAnimator = null;
    private List<Animator> commonAnimators = null;
    
    private Vector3 left = new Vector3(-0.6f, 0.6f, 1f);
    private Vector3 right = new Vector3(0.6f, 0.6f, 1f);
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
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        if (horizontal < 0) {
            transform.localScale = left;
        }
        if (horizontal > 0) {
            transform.localScale = right;
        }
        if (horizontal != 0f || vertical != 0f) {
            float positionDeltaX = horizontal * xVelocity;
            float positionDeltaY = vertical * yVelocity;
            float x = Math.Clamp(transform.position.x + positionDeltaX, xMin, xMax);
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
        
    }
}
