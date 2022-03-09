﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Realtime;
using UnityEngine.UI;

public class PlayerController : MonoBehaviourPunCallbacks, IDamagable
{
	// UI
	[SerializeField] Image healthbarImage;
	[SerializeField] GameObject ui;

	// Camera
	[SerializeField] GameObject cameraHolder;
	float verticalLookRotation;

	// Variables for moving up and down stairs
	[SerializeField] GameObject stepRayLower;
	[SerializeField] GameObject stepRayUpper;
	[SerializeField] float stepHeight = 0.3f;
	[SerializeField] float stepSmooth = 0.1f;
	
	// controls and movement
	[SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;
	bool grounded;
	Vector3 smoothMoveVelocity;
	Vector3 moveAmount;

	// items and guns
	[SerializeField] Item[] items;
	int itemIndex;
	int previousItemIndex = -1;

	// health
	const float maxHealh = 100f;
	float currentHealth = maxHealh;

	// components and classes
	Rigidbody rb;
	PhotonView PV;
	PlayerManager playerManager;
	
	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		PV = GetComponent<PhotonView>();
		
		playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();

		stepRayUpper.transform.position = new Vector3(stepRayUpper.transform.position.x, stepHeight, stepRayUpper.transform.position.z);
	}
	
	void Start()
	{
		if(PV.IsMine)
		{
			EquipItem(0);
		}
		else
		{
			Destroy(GetComponentInChildren<Camera>().gameObject);
			Destroy(rb);
			Destroy(ui);
		}
	}
	
	void Update()
	{
		if(!PV.IsMine)
			return;
			
		Look();
		Move();
		Jump();
		stepClimb();
		
		for(int i = 0; i < items.Length; i++)
		{
			if(Input.GetKeyDown((i + 1).ToString()))
			{
				EquipItem(i);
				break;
			}
		}
		
		if(Input.GetAxisRaw("Mouse Scrollwheel") > 0f)
		{
			if(itemIndex >= items.Length - 1)
			{
				EquipItem(0);
			} 
			else 
			{
				EquipItem(itemIndex + 1);
			}
		}
		else if(Input.GetAxisRaw("Mouse Scrollwheel") < 0f)
		{
			if(itemIndex <= 0)
			{
				EquipItem(items.Length - 1);
			} else 
			{
				EquipItem(itemIndex - 1);
			}
		}
		
		if(Input.GetMouseButtonDown(0))
		{
			items[itemIndex].Use();
		}
		
		if(transform.position.y < -10f)
        {
			Die();
        }
		
		if(PV.IsMine)
		{
			Hashtable hash = new Hashtable();
			hash.Add("itemIndex", itemIndex);
			PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
		}
	}
	
	public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
	{
		if(!PV.IsMine && targetPlayer == PV.Owner)
		{
			EquipItem((int)changedProps["itemIndex"]);
		}
	}
	
	void Look()
	{
		transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * mouseSensitivity);
		
		verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
		verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
		
		cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
	}
	
	void Move()
	{
		Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
		
		moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
	}
	
	void Jump()
	{
		if(Input.GetKeyDown(KeyCode.Space) && grounded)
		{
			rb.AddForce(transform.up * jumpForce);
		}
	}
	
	void EquipItem(int _index)
	{
		if(_index == previousItemIndex)
			return;
		
		itemIndex = _index;
		
		items[itemIndex].itemGameObject.SetActive(true);
		
		if(previousItemIndex != -1)
		{
			items[previousItemIndex].itemGameObject.SetActive(false);
		}
		
		previousItemIndex = itemIndex;
	}
	
	public void SetGroundedState(bool _grounded)
	{
		grounded = _grounded;
	}

    

    void FixedUpdate()
	{
		if(!PV.IsMine)
			return;
		rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
	}
	
	public void TakeDamage(float damage)
	{
		PV.RPC("RPC_TakeDamage", RpcTarget.All, damage);
	}

	void stepClimb()
    {
		RaycastHit hitLower;
		if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLower, 0.1f))
        {
			RaycastHit hitUpper;
			if (!Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitUpper, 0.1f))
				{
					rb.position -= new Vector3(0f, -stepSmooth, 0f);
				}
        }

		RaycastHit hitLower45;
		if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLower45, 0.1f))
		{
			RaycastHit hitUpper45;
			if (!Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitUpper45, 0.1f))
			{
				rb.position -= new Vector3(0f, -stepSmooth, 0f);
			}
		}

		RaycastHit hitLowerMinus45;
		if (Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitLowerMinus45, 0.1f))
		{
			RaycastHit hitUpperMinus45;
			if (!Physics.Raycast(stepRayLower.transform.position, transform.TransformDirection(Vector3.forward), out hitUpperMinus45, 0.1f))
			{
				rb.position -= new Vector3(0f, -stepSmooth, 0f);
			}
		}
	}
	
	[PunRPC]
	void RPC_TakeDamage(float damage)
	{
		if(!PV.IsMine)
			return;
			
		currentHealth -= damage;

		healthbarImage.fillAmount = currentHealth / maxHealh;
		
		Debug.Log("took " + damage + " damage");
		
		if(currentHealth <= 0)
		{
			Die();
		}
	}
	
	void Die()
	{
		playerManager.Die();
	}

}
