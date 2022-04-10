using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using TMPro;
using System.Text.RegularExpressions;

public class playerMovement : MonoBehaviour
{
    //please dont judge this mess 
    public CharacterController controller;
    public float speed = 6f;
    public float fruitSpeed = 17;
    public float turnsmoothTime = 0.1f;
    public float turnsmoothVelocity;
    public float gravity = -9.81f;
    public float groundDist = 0.4f;
    public float maxFruitSpeed = 5;
    Vector3 velocity;
    public Transform cam;
    public Transform groundCheck;
    public Animator animator;
    public LayerMask groundMask;
    bool isGrounded;
    bool isFruitCollected;
    bool isfruitForm;
    public bool isHunter;
    public List<GameObject> allFruits = new List<GameObject>();
    private Vector3 movedir;
    public GameObject fruitsContainer;
    public GameObject nearestfruitrn;
    public GameObject myfruit;
    private GameObject humanForm;
    public GameObject cmthing;
    Rigidbody fruitRB;
    float shirtR;
    float shirtG;
    float shirtB;
    PhotonView view;
    public TMP_Text topleft;
    public Material HunterMaterial;
    public Material HunterShoeMat;
    string GameState = "";
    private void Start()
    {
        view = GetComponent<PhotonView>();
        if (!view.IsMine) return;
        fruitsContainer = GameObject.Find("fruitsContainer");
        cam = GameObject.Find("3rdpersonCam").transform;
        groundCheck = gameObject.transform.GetChild(0).GetChild(5).gameObject.transform;
        cmthing = GameObject.Find("CMthing");
        animator = gameObject.transform.GetChild(0).gameObject.GetComponent<Animator>();
        //add hunters animator here.
        shirtR = Random.Range(0f, 1f);
        shirtG = Random.Range(0f, 1f);
        shirtB = Random.Range(0f, 1f);
        view.RPC("ChangeColor", RpcTarget.AllBuffered, shirtR, shirtG, shirtB);
        
        humanForm = gameObject.transform.GetChild(0).gameObject;
        cmthing.transform.position = transform.position + new Vector3(0, 1.55f, 0);
        cmthing.transform.parent = transform;
        foreach (Transform fruit in fruitsContainer.transform)
        {
            allFruits.Add(fruit.gameObject);
        }
        topleft = GameObject.Find("Canvas").transform.GetChild(0).GetComponent<TMP_Text>();
    }
    void Update()
    {
        if(PhotonNetwork.CurrentRoom.PlayerCount >= 5)
        {
            if (PhotonNetwork.CurrentRoom.Players[1 + (PhotonNetwork.CurrentRoom.Name.Length % 4)] == PhotonNetwork.LocalPlayer)
            {
                isHunter = true;
                topleft.text = " HUNT THEM ALL, YOU ARE POSSESSED";
                topleft.color = Color.red;
            }
            else GameState = " The hunter is active.";
        }
        if (view.IsMine)
        {
            if (!isHunter)
            {
                nearestfruitrn = GetnearestFruit(allFruits);
                if (nearestfruitrn != null && !isFruitCollected) topleft.text = "Hit F or Space to pick up " + StripPrefabNumber(nearestfruitrn.name) + GameState;
                if (nearestfruitrn == null && !isFruitCollected) topleft.text = "Room Code: '" + PhotonNetwork.CurrentRoom.Name + "'    Currently at " + PhotonNetwork.CurrentRoom.PlayerCount + "/5 players." + GameState;
                if (!isfruitForm && isFruitCollected) topleft.text = "Hit shift to shift into " + StripPrefabNumber(myfruit.name) + GameState;
                if (isfruitForm) topleft.text = "Hit shift to shift back to Human Form " + GameState;
                if ((myfruit == null) && (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space)))
                {
                    view.RPC("PickFruit", RpcTarget.All);
                }
                if (isfruitForm && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
                {
                    view.RPC("ShiftTwo", RpcTarget.All);
                }
                if (isFruitCollected && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
                {
                    view.RPC("ShiftOne", RpcTarget.All);
                }
            }
            else
            {
                //hunter specific mechanics code goes here.
                speed = 11f;
                view.RPC("HunterBlack", RpcTarget.All);
            }
            if (isfruitForm)
            {
                //rigidbody movement for fruitform.
                float horizontal = Input.GetAxisRaw("Horizontal");
                float vertical = Input.GetAxisRaw("Vertical");
                movedir = cmthing.transform.position - cam.transform.position;
                fruitRB.AddForce(new Vector3(movedir.x, 0, movedir.z).normalized * vertical * fruitSpeed);
                fruitRB.AddForce(new Vector3(movedir.z, 0, -movedir.x).normalized * horizontal * fruitSpeed);
                fruitRB.velocity = Vector3.ClampMagnitude(fruitRB.velocity, maxFruitSpeed);
            }
            else
            {
                SurvivorMovement();
            }
        }
    }
    private void SurvivorMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.transform.position, groundDist, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (isHunter) animator.SetFloat("HunterSpeed", Mathf.Abs(horizontal) + Mathf.Abs(vertical));
        else animator.SetFloat("Speed", Mathf.Abs(horizontal) + Mathf.Abs(vertical));
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnsmoothVelocity, turnsmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    private GameObject GetnearestFruit(List<GameObject> allFruits)
    {
        float smallestDistsqr = 9f;
        GameObject nearestFruit = null;
        Vector3 currentPos = transform.position;
        foreach (var currentFruit in allFruits)
        {
            Vector3 directionToFruit = currentFruit.transform.position - currentPos;
            float dsqrToFruit = directionToFruit.sqrMagnitude;
            if (dsqrToFruit < smallestDistsqr)
            {
                smallestDistsqr = dsqrToFruit;
                nearestFruit = currentFruit;
            }
        }
        //highlight the nearest fruit here.
        return nearestFruit;
    }
    public static string StripPrefabNumber(string word)
    {
        Regex reg = new Regex("[^a-zA-Z']");
        return reg.Replace(word, string.Empty);
    }
    [PunRPC]
    void ChangeColor(float shirtR, float shirtG, float shirtB)
    {
        gameObject.transform.GetChild(0).GetChild(3).gameObject.GetComponent<Renderer>().material.color = new Color(shirtR, shirtG, shirtB);
    }
    [PunRPC]
    void HunterBlack()
    {
        for (int i = 1; i < 5; i++)
        {
            gameObject.transform.GetChild(0).GetChild(i).gameObject.GetComponent<Renderer>().material = HunterMaterial;
        }
        gameObject.transform.GetChild(0).GetChild(5).gameObject.GetComponent<Renderer>().material = HunterShoeMat;
    }
    [PunRPC]
    void PickFruit()
    {
        myfruit = nearestfruitrn;
        allFruits.Remove(myfruit);
        myfruit.SetActive(false);
        isFruitCollected = true;
    }
    [PunRPC]
    void ShiftOne()
    {
        myfruit.SetActive(true);
        fruitRB = myfruit.gameObject.GetComponent<Rigidbody>();
        myfruit.transform.position = transform.position;
        humanForm.SetActive(false);
        isfruitForm = true;
        cmthing.transform.position = myfruit.transform.position;
        cmthing.transform.parent = myfruit.transform;
    }
    [PunRPC]
    void ShiftTwo()
    {
        humanForm.SetActive(true);
        controller.enabled = false;
        transform.position = myfruit.transform.position;
        controller.enabled = true;
        myfruit.SetActive(false);
        myfruit = null;
        Destroy(myfruit);
        isFruitCollected = false;
        fruitRB = null;
        isfruitForm = false;
        cmthing.transform.position = transform.position + new Vector3(0, 1.55f, 0);
        cmthing.transform.parent = transform;
    }
}

