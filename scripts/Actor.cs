// Actor
using System;
using UnityEngine;

public abstract class Actor : MonoBehaviour
{
	public Transform gunPosition;

	public Transform orientation;

	private float xRotation;

	private Rigidbody rb;

	private float accelerationSpeed = 4500f;

	private float maxSpeed = 20f;

	private bool crouching;

	private bool jumping;

	private bool wallRunning;

	protected float x;

	protected float y;

	private Vector3 wallNormalVector = Vector3.up;

	private bool grounded;

	public Transform groundChecker;

	public LayerMask whatIsGround;

	private bool readyToJump;

	private float jumpCooldown = 0.2f;

	private float jumpForce = 500f;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		OnStart();
	}

	private void FixedUpdate()
	{
		Movement();
		RotateBody();
	}

	private void LateUpdate()
	{
		Look();
	}

	private void Update()
	{
		Logic();
	}

	protected abstract void OnStart();

	protected abstract void Logic();

	protected abstract void RotateBody();

	protected abstract void Look();

	private void Movement()
	{
		grounded = (Physics.OverlapSphere(groundChecker.position, 0.1f, whatIsGround).Length != 0);
		Vector2 mag = FindVelRelativeToLook();
		float num = mag.x;
		float num2 = mag.y;
		CounterMovement(x, y, mag);
		if (readyToJump && jumping)
		{
			Jump();
		}
		if (crouching && grounded && readyToJump)
		{
			rb.AddForce(Vector3.down * Time.deltaTime * 2000f);
			return;
		}
		if (x > 0f && num > maxSpeed)
		{
			x = 0f;
		}
		if (x < 0f && num < 0f - maxSpeed)
		{
			x = 0f;
		}
		if (y > 0f && num2 > maxSpeed)
		{
			y = 0f;
		}
		if (y < 0f && num2 < 0f - maxSpeed)
		{
			y = 0f;
		}
		rb.AddForce(Time.deltaTime * y * accelerationSpeed * orientation.transform.forward);
		rb.AddForce(Time.deltaTime * x * accelerationSpeed * orientation.transform.right);
	}

	private void Jump()
	{
		if (grounded || wallRunning)
		{
			Vector3 velocity = rb.velocity;
			rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
			readyToJump = false;
			rb.AddForce(Vector2.up * jumpForce * 1.5f);
			rb.AddForce(wallNormalVector * jumpForce * 0.5f);
			Invoke("ResetJump", jumpCooldown);
			if (wallRunning)
			{
				wallRunning = false;
			}
		}
	}

	private void ResetJump()
	{
		readyToJump = true;
	}

	protected void CounterMovement(float x, float y, Vector2 mag)
	{
		if (grounded && !crouching)
		{
			float num = 0.2f;
			if (x == 0f || (mag.x < 0f && x > 0f) || (mag.x > 0f && x < 0f))
			{
				rb.AddForce(accelerationSpeed * num * Time.deltaTime * (0f - mag.x) * orientation.transform.right);
			}
			if (y == 0f || (mag.y < 0f && y > 0f) || (mag.y > 0f && y < 0f))
			{
				rb.AddForce(accelerationSpeed * num * Time.deltaTime * (0f - mag.y) * orientation.transform.forward);
			}
			if (Mathf.Sqrt(Mathf.Pow(rb.velocity.x, 2f) + Mathf.Pow(rb.velocity.z, 2f)) > 20f)
			{
				float num2 = rb.velocity.y;
				Vector3 vector = rb.velocity.normalized * 20f;
				rb.velocity = new Vector3(vector.x, num2, vector.z);
			}
		}
	}

	protected Vector2 FindVelRelativeToLook()
	{
		float current = orientation.transform.eulerAngles.y;
		Vector3 velocity = rb.velocity;
		float target = Mathf.Atan2(velocity.x, velocity.z) * 57.29578f;
		float num = Mathf.DeltaAngle(current, target);
		float num2 = 90f - num;
		float magnitude = rb.velocity.magnitude;
		return new Vector2(y: magnitude * Mathf.Cos(num * ((float)Math.PI / 180f)), x: magnitude * Mathf.Cos(num2 * ((float)Math.PI / 180f)));
	}
}

// Barrel
using UnityEngine;

public class Barrel : MonoBehaviour
{
	private bool done;

	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Bullet"))
		{
			Explosion explosion = (Explosion)UnityEngine.Object.Instantiate(PrefabManager.Instance.explosion, base.transform.position, Quaternion.identity).GetComponentInChildren(typeof(Explosion));
			UnityEngine.Object.Destroy(base.gameObject);
			CancelInvoke();
			done = true;
			Bullet bullet = (Bullet)other.gameObject.GetComponent(typeof(Bullet));
			if ((bool)bullet && bullet.player)
			{
				explosion.player = bullet.player;
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Bullet"))
		{
			done = true;
			Invoke("Explode", 0.2f);
		}
	}

	private void Explode()
	{
		UnityEngine.Object.Instantiate(PrefabManager.Instance.explosion, base.transform.position, Quaternion.identity);
		UnityEngine.Object.Destroy(base.gameObject);
	}
}

// Bounce
using UnityEngine;

public class Bounce : MonoBehaviour
{
	private void OnCollisionEnter(Collision other)
	{
		MonoBehaviour.print("yeet");
		_ = (bool)other.gameObject.GetComponent<Rigidbody>();
	}
}

// Break
using UnityEngine;

public class Break : MonoBehaviour
{
	public GameObject replace;

	private bool done;

	private void OnCollisionEnter(Collision other)
	{
		if (done || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
		{
			return;
		}
		Rigidbody component = other.gameObject.GetComponent<Rigidbody>();
		if (!component || !(component.velocity.magnitude > 18f))
		{
			return;
		}
		if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
		{
			if (!PlayerMovement.Instance.IsCrouching())
			{
				return;
			}
			PlayerMovement.Instance.Slowmo(0.35f, 0.8f);
			BreakDoor(component);
		}
		BreakDoor(component);
	}

	private void BreakDoor(Rigidbody rb)
	{
		Vector3 velocity = rb.velocity;
		float magnitude = velocity.magnitude;
		if (magnitude > 20f)
		{
			float num = magnitude / 20f;
			velocity /= num;
		}
		Rigidbody[] componentsInChildren = UnityEngine.Object.Instantiate(replace, base.transform.position, base.transform.rotation).GetComponentsInChildren<Rigidbody>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].velocity = velocity * 1.5f;
		}
		UnityEngine.Object.Instantiate(PrefabManager.Instance.destructionAudio, base.transform.position, Quaternion.identity);
		UnityEngine.Object.Destroy(base.gameObject);
		done = true;
	}
}

// Bullet
using Audio;
using UnityEngine;

public class Bullet : MonoBehaviour
{
	public bool changeCol;

	public bool player;

	private float damage;

	private float push;

	private bool done;

	private Color col;

	public bool explosive;

	private GameObject limbHit;

	private Rigidbody rb;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void OnCollisionEnter(Collision other)
	{
		if (done)
		{
			return;
		}
		done = true;
		if (explosive)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			((Explosion)UnityEngine.Object.Instantiate(PrefabManager.Instance.explosion, other.contacts[0].point, Quaternion.identity).GetComponentInChildren(typeof(Explosion))).player = player;
			return;
		}
		BulletExplosion(other.contacts[0]);
		UnityEngine.Object.Instantiate(PrefabManager.Instance.bulletHitAudio, other.contacts[0].point, Quaternion.identity);
		int layer = other.gameObject.layer;
		if (layer == LayerMask.NameToLayer("Player"))
		{
			HitPlayer(other.gameObject);
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		if (layer == LayerMask.NameToLayer("Enemy"))
		{
			if (col == Color.blue)
			{
				AudioManager.Instance.Play("Hitmarker");
				MonoBehaviour.print("HITMARKER");
			}
			UnityEngine.Object.Instantiate(PrefabManager.Instance.enemyHitAudio, other.contacts[0].point, Quaternion.identity);
			((RagdollController)other.transform.root.GetComponent(typeof(RagdollController))).MakeRagdoll(-base.transform.right * 350f);
			if ((bool)other.gameObject.GetComponent<Rigidbody>())
			{
				other.gameObject.GetComponent<Rigidbody>().AddForce(-base.transform.right * 1500f);
			}
			((Enemy)other.transform.root.GetComponent(typeof(Enemy))).DropGun(Vector3.up);
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		if (layer == LayerMask.NameToLayer("Bullet"))
		{
			if (other.gameObject.name == base.gameObject.name)
			{
				return;
			}
			UnityEngine.Object.Destroy(base.gameObject);
			UnityEngine.Object.Destroy(other.gameObject);
			BulletExplosion(other.contacts[0]);
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void HitPlayer(GameObject other)
	{
		PlayerMovement.Instance.KillPlayer();
	}

	private void Update()
	{
		if (explosive)
		{
			rb.AddForce(Vector3.up * Time.deltaTime * 1000f);
		}
	}

	private void BulletExplosion(ContactPoint contact)
	{
		Vector3 point = contact.point;
		Vector3 normal = contact.normal;
		ParticleSystem component = UnityEngine.Object.Instantiate(PrefabManager.Instance.bulletDestroy, point + normal * 0.05f, Quaternion.identity).GetComponent<ParticleSystem>();
		component.transform.rotation = Quaternion.LookRotation(normal);
		component.startColor = Color.blue;
	}

	public void SetBullet(float damage, float push, Color col)
	{
		this.damage = damage;
		this.push = push;
		this.col = col;
		if (changeCol)
		{
			SpriteRenderer[] componentsInChildren = GetComponentsInChildren<SpriteRenderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].color = col;
			}
		}
		TrailRenderer componentInChildren = GetComponentInChildren<TrailRenderer>();
		if (!(componentInChildren == null))
		{
			componentInChildren.startColor = col;
			componentInChildren.endColor = col;
		}
	}
}

// Debug
using TMPro;
using UnityEngine;

public class Debug : MonoBehaviour
{
	public TextMeshProUGUI fps;

	public TMP_InputField console;

	public TextMeshProUGUI consoleLog;

	private bool fpsOn;

	private bool speedOn;

	private float deltaTime;

	private void Start()
	{
		Application.targetFrameRate = 150;
	}

	private void Update()
	{
		Fps();
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			if (console.isActiveAndEnabled)
			{
				CloseConsole();
			}
			else
			{
				OpenConsole();
			}
		}
	}

	private void Fps()
	{
		if (!fpsOn && !speedOn)
		{
			if (!fps.enabled)
			{
				fps.gameObject.SetActive(value: false);
			}
			return;
		}
		if (!fps.gameObject.activeInHierarchy)
		{
			fps.gameObject.SetActive(value: true);
		}
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
		float num = deltaTime * 1000f;
		float num2 = 1f / deltaTime;
		string text = "";
		if (fpsOn)
		{
			text += $"{num:0.0} ms ({num2:0.} fps)";
		}
		if (speedOn)
		{
			text = text + "\nm/s: " + $"{PlayerMovement.Instance.rb.velocity.magnitude:F1}";
		}
		fps.text = text;
	}

	private void OpenConsole()
	{
		console.gameObject.SetActive(value: true);
		console.Select();
		console.ActivateInputField();
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		PlayerMovement.Instance.paused = true;
		Time.timeScale = 0f;
	}

	private void CloseConsole()
	{
		console.gameObject.SetActive(value: false);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		PlayerMovement.Instance.paused = false;
		Time.timeScale = 1f;
	}

	public void RunCommand()
	{
		string text = console.text;
		TextMeshProUGUI textMeshProUGUI = consoleLog;
		textMeshProUGUI.text = textMeshProUGUI.text + text + "\n";
		if (text.Length < 2 || text.Length > 30 || CountWords(text) != 2)
		{
			console.text = "";
			console.Select();
			console.ActivateInputField();
			return;
		}
		console.text = "";
		string s = text.Substring(text.IndexOf(' ') + 1);
		string text2 = text.Substring(0, text.IndexOf(' '));
		if (!int.TryParse(s, out int result))
		{
			consoleLog.text += "Command not found\n";
			return;
		}
		switch (text2)
		{
		case "fps":
			OpenCloseFps(result);
			break;
		case "fpslimit":
			FpsLimit(result);
			break;
		case "fov":
			ChangeFov(result);
			break;
		case "sens":
			ChangeSens(result);
			break;
		case "speed":
			OpenCloseSpeed(result);
			break;
		case "help":
			Help();
			break;
		}
		console.Select();
		console.ActivateInputField();
	}

	private void Help()
	{
		string text = "The console can be used for simple commands.\nEvery command must be followed by number i (0 = false, 1 = true)\n<i><b>fps 1</b></i>            shows fps\n<i><b>speed 1</b></i>      shows speed\n<i><b>fov i</b></i>             sets fov to i\n<i><b>sens i</b></i>          sets sensitivity to i\n<i><b>fpslimit i</b></i>    sets max fps\n<i><b>TAB</b></i>              to open/close the console\n";
		consoleLog.text += text;
	}

	private void FpsLimit(int n)
	{
		Application.targetFrameRate = n;
		TextMeshProUGUI textMeshProUGUI = consoleLog;
		textMeshProUGUI.text = textMeshProUGUI.text + "Max FPS set to " + n + "\n";
	}

	private void OpenCloseFps(int n)
	{
		fpsOn = (n == 1);
		consoleLog.text += ("FPS set to " + n == 1 + "\n");
	}

	private void OpenCloseSpeed(int n)
	{
		speedOn = (n == 1);
		consoleLog.text += ("Speedometer set to " + n == 1 + "\n");
	}

	private void ChangeFov(int n)
	{
		GameState.Instance.SetFov(n);
		TextMeshProUGUI textMeshProUGUI = consoleLog;
		textMeshProUGUI.text = textMeshProUGUI.text + "FOV set to " + n + "\n";
	}

	private void ChangeSens(int n)
	{
		GameState.Instance.SetSensitivity(n);
		TextMeshProUGUI textMeshProUGUI = consoleLog;
		textMeshProUGUI.text = textMeshProUGUI.text + "Sensitivity set to " + n + "\n";
	}

	private int CountWords(string text)
	{
		int num = 0;
		int i;
		for (i = 0; i < text.Length && char.IsWhiteSpace(text[i]); i++)
		{
		}
		while (i < text.Length)
		{
			for (; i < text.Length && !char.IsWhiteSpace(text[i]); i++)
			{
			}
			num++;
			for (; i < text.Length && char.IsWhiteSpace(text[i]); i++)
			{
			}
		}
		return num;
	}
}

// DestroyObject
using UnityEngine;

public class DestroyObject : MonoBehaviour
{
	public float time;

	private void Start()
	{
		Invoke("DestroySelf", time);
	}

	private void DestroySelf()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}
}

// DetectWeapons
using Audio;
using System.Collections.Generic;
using UnityEngine;

public class DetectWeapons : MonoBehaviour
{
	public Transform weaponPos;

	private List<GameObject> weapons;

	private bool hasGun;

	private GameObject gun;

	private Pickup gunScript;

	private float speed = 10f;

	private Quaternion desiredRot = Quaternion.Euler(0f, 90f, 0f);

	private Vector3 desiredPos = Vector3.zero;

	private Vector3 posVel;

	private float throwForce = 1000f;

	private Vector3 scale;

	public void Pickup()
	{
		if (!hasGun && HasWeapons())
		{
			gun = GetWeapon();
			gunScript = (Pickup)gun.GetComponent(typeof(Pickup));
			if (gunScript.pickedUp)
			{
				gun = null;
				gunScript = null;
				return;
			}
			UnityEngine.Object.Destroy(gun.GetComponent<Rigidbody>());
			scale = gun.transform.localScale;
			gun.transform.parent = weaponPos;
			gun.transform.localScale = scale;
			hasGun = true;
			gunScript.PickupWeapon(player: true);
			AudioManager.Instance.Play("GunPickup");
			gun.layer = LayerMask.NameToLayer("Equipable");
		}
	}

	public void Shoot(Vector3 dir)
	{
		if (hasGun)
		{
			gunScript.Use(dir);
		}
	}

	public void StopUse()
	{
		if (hasGun)
		{
			gunScript.StopUse();
		}
	}

	public void Throw(Vector3 throwDir)
	{
		if (hasGun && !gun.GetComponent<Rigidbody>())
		{
			gunScript.StopUse();
			hasGun = false;
			Rigidbody rigidbody = gun.AddComponent<Rigidbody>();
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.maxAngularVelocity = 20f;
			rigidbody.AddForce(throwDir * throwForce);
			rigidbody.AddRelativeTorque(new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f) * 0.4f), ForceMode.Impulse);
			gun.layer = LayerMask.NameToLayer("Gun");
			gunScript.Drop();
			gun.transform.parent = null;
			gun.transform.localScale = scale;
			gun = null;
			gunScript = null;
		}
	}

	public void Fire(Vector3 dir)
	{
		gunScript.Use(dir);
	}

	private void Update()
	{
		if (hasGun)
		{
			gun.transform.localRotation = Quaternion.Slerp(gun.transform.localRotation, desiredRot, Time.deltaTime * speed);
			gun.transform.localPosition = Vector3.SmoothDamp(gun.transform.localPosition, desiredPos, ref posVel, 1f / speed);
			gunScript.OnAim();
		}
	}

	private void Start()
	{
		weapons = new List<GameObject>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Gun") && !weapons.Contains(other.gameObject))
		{
			weapons.Add(other.gameObject);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Gun") && weapons.Contains(other.gameObject))
		{
			weapons.Remove(other.gameObject);
		}
	}

	public GameObject GetWeapon()
	{
		if (weapons.Count == 1)
		{
			return weapons[0];
		}
		GameObject result = null;
		float num = float.PositiveInfinity;
		foreach (GameObject weapon in weapons)
		{
			float num2 = Vector3.Distance(base.transform.position, weapon.transform.position);
			if (num2 < num)
			{
				num = num2;
				result = weapon;
			}
		}
		return result;
	}

	public void ForcePickup(GameObject gun)
	{
		gunScript = (Pickup)gun.GetComponent(typeof(Pickup));
		this.gun = gun;
		if (gunScript.pickedUp)
		{
			gun = null;
			gunScript = null;
			return;
		}
		UnityEngine.Object.Destroy(gun.GetComponent<Rigidbody>());
		scale = gun.transform.localScale;
		gun.transform.parent = weaponPos;
		gun.transform.localScale = scale;
		hasGun = true;
		gunScript.PickupWeapon(player: true);
		gun.layer = LayerMask.NameToLayer("Equipable");
	}

	public float GetRecoil()
	{
		return gunScript.recoil;
	}

	public bool HasWeapons()
	{
		return weapons.Count > 0;
	}

	public bool IsGrappler()
	{
		if (!gun)
		{
			return false;
		}
		return gun.GetComponent(typeof(Grappler));
	}

	public Vector3 GetGrapplerPoint()
	{
		if (IsGrappler())
		{
			return ((Grappler)gun.GetComponent(typeof(Grappler))).GetGrapplePoint();
		}
		return Vector3.zero;
	}

	public Pickup GetWeaponScript()
	{
		return gunScript;
	}

	public bool HasGun()
	{
		return hasGun;
	}
}

// Enemy
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
	private float hipSpeed = 3f;

	private float headAndHandSpeed = 4f;

	private Transform target;

	public LayerMask objectsAndPlayer;

	private NavMeshAgent agent;

	private bool spottedPlayer;

	private Animator animator;

	public GameObject startGun;

	public Transform gunPosition;

	private Weapon gunScript;

	public GameObject currentGun;

	private float attackSpeed;

	private bool readyToShoot;

	private RagdollController ragdoll;

	public Transform leftArm;

	public Transform rightArm;

	public Transform head;

	public Transform hips;

	public Transform player;

	private bool takingAim;

	private void Start()
	{
		ragdoll = (RagdollController)GetComponent(typeof(RagdollController));
		animator = GetComponentInChildren<Animator>();
		agent = GetComponent<NavMeshAgent>();
		GiveGun();
	}

	private void LateUpdate()
	{
		FindPlayer();
		Aim();
	}

	private void Aim()
	{
		if (!(currentGun == null) && !ragdoll.IsRagdoll() && animator.GetBool("Aiming"))
		{
			Vector3 vector = target.transform.position - base.transform.position;
			if (Vector3.Angle(base.transform.forward, vector) > 70f)
			{
				base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation(vector), Time.deltaTime * hipSpeed);
			}
			head.transform.rotation = Quaternion.Slerp(head.transform.rotation, Quaternion.LookRotation(vector), Time.deltaTime * headAndHandSpeed);
			rightArm.transform.rotation = Quaternion.Slerp(head.transform.rotation, Quaternion.LookRotation(vector), Time.deltaTime * headAndHandSpeed);
			leftArm.transform.rotation = Quaternion.Slerp(head.transform.rotation, Quaternion.LookRotation(vector), Time.deltaTime * headAndHandSpeed);
			if (readyToShoot)
			{
				gunScript.Use(target.position);
				readyToShoot = false;
				Invoke("Cooldown", attackSpeed + Random.Range(attackSpeed, attackSpeed * 5f));
			}
		}
	}

	private void FindPlayer()
	{
		FindTarget();
		if (!agent || !target)
		{
			return;
		}
		Vector3 normalized = (target.position - base.transform.position).normalized;
		RaycastHit[] array = Physics.RaycastAll(base.transform.position + normalized, normalized, (int)objectsAndPlayer);
		if (array.Length < 1)
		{
			return;
		}
		bool flag = false;
		float num = 1001f;
		float num2 = 1000f;
		for (int i = 0; i < array.Length; i++)
		{
			int layer = array[i].collider.gameObject.layer;
			if (!(array[i].collider.transform.root.gameObject.name == base.gameObject.name) && layer != LayerMask.NameToLayer("TransparentFX"))
			{
				if (layer == LayerMask.NameToLayer("Player"))
				{
					num = array[i].distance;
					flag = true;
				}
				else if (array[i].distance < num2)
				{
					num2 = array[i].distance;
				}
			}
		}
		if (!flag)
		{
			return;
		}
		if (num2 < num && num != 1001f)
		{
			readyToShoot = false;
			if (animator.GetBool("Running") && agent.remainingDistance < 0.2f)
			{
				animator.SetBool("Running", value: false);
				spottedPlayer = false;
			}
			if (spottedPlayer && agent.isOnNavMesh && !animator.GetBool("Running"))
			{
				MonoBehaviour.print("oof");
				takingAim = false;
				agent.destination = target.transform.position;
				animator.SetBool("Running", value: true);
				animator.SetBool("Aiming", value: false);
				readyToShoot = false;
			}
		}
		else if (!takingAim && !animator.GetBool("Aiming"))
		{
			if (!spottedPlayer)
			{
				spottedPlayer = true;
			}
			Invoke("TakeAim", Random.Range(0.3f, 1f));
			takingAim = true;
		}
	}

	private void TakeAim()
	{
		animator.SetBool("Running", value: false);
		animator.SetBool("Aiming", value: true);
		CancelInvoke();
		Invoke("Cooldown", Random.Range(0.3f, 1f));
		if ((bool)agent && agent.isOnNavMesh)
		{
			agent.destination = base.transform.position;
		}
	}

	private void GiveGun()
	{
		if (!(startGun == null))
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(startGun);
			UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
			gunScript = (Weapon)gameObject.GetComponent(typeof(Weapon));
			gunScript.PickupWeapon(player: false);
			gameObject.transform.parent = gunPosition;
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;
			currentGun = gameObject;
			attackSpeed = gunScript.GetAttackSpeed();
		}
	}

	private void Cooldown()
	{
		readyToShoot = true;
	}

	private void FindTarget()
	{
		if (!(target != null) && (bool)PlayerMovement.Instance)
		{
			target = PlayerMovement.Instance.playerCam;
		}
	}

	public void DropGun(Vector3 dir)
	{
		if (!(gunScript == null))
		{
			gunScript.Drop();
			Rigidbody rigidbody = currentGun.AddComponent<Rigidbody>();
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			currentGun.transform.parent = null;
			rigidbody.AddForce(dir, ForceMode.Impulse);
			float d = 10f;
			rigidbody.AddTorque(new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)) * d);
			gunScript = null;
		}
	}

	public bool IsDead()
	{
		return ragdoll.IsRagdoll();
	}
}

// Explosion
using EZCameraShake;
using UnityEngine;

public class Explosion : MonoBehaviour
{
	public bool player;

	private void Start()
	{
		float num = Vector3.Distance(base.transform.position, PlayerMovement.Instance.gameObject.transform.position);
		MonoBehaviour.print(num);
		float num2 = 10f / num;
		if (num2 < 0.1f)
		{
			num2 = 0f;
		}
		CameraShaker.Instance.ShakeOnce(20f * num2 * GameState.Instance.cameraShake, 2f, 0.4f, 0.5f);
		MonoBehaviour.print("ratio: " + num2);
	}

	private void OnTriggerEnter(Collider other)
	{
		int layer = other.gameObject.layer;
		Vector3 normalized = (other.transform.position - base.transform.position).normalized;
		float num = Vector3.Distance(other.transform.position, base.transform.position);
		if (other.gameObject.CompareTag("Enemy"))
		{
			if (other.gameObject.name != "Torso")
			{
				return;
			}
			RagdollController ragdollController = (RagdollController)other.transform.root.GetComponent(typeof(RagdollController));
			if ((bool)ragdollController && !ragdollController.IsRagdoll())
			{
				ragdollController.MakeRagdoll(normalized * 1100f);
				if (player)
				{
					PlayerMovement.Instance.Slowmo(0.35f, 0.5f);
				}
				Enemy enemy = (Enemy)other.transform.root.GetComponent(typeof(Enemy));
				if ((bool)enemy)
				{
					enemy.DropGun(Vector3.up);
				}
			}
			return;
		}
		Rigidbody component = other.gameObject.GetComponent<Rigidbody>();
		if ((bool)component)
		{
			if (num < 5f)
			{
				num = 5f;
			}
			component.AddForce(normalized * 450f / num, ForceMode.Impulse);
			component.AddTorque(new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10f);
			if (layer == LayerMask.NameToLayer("Player"))
			{
				((PlayerMovement)other.transform.root.GetComponent(typeof(PlayerMovement))).Explode();
			}
		}
	}
}

// ExplosiveBullet
using UnityEngine;

public class ExplosiveBullet : MonoBehaviour
{
	private Rigidbody rb;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		UnityEngine.Object.Instantiate(PrefabManager.Instance.thumpAudio, base.transform.position, Quaternion.identity);
	}

	private void OnCollisionEnter(Collision other)
	{
		UnityEngine.Object.Destroy(base.gameObject);
		UnityEngine.Object.Instantiate(PrefabManager.Instance.explosion, base.transform.position, Quaternion.identity);
	}

	private void Update()
	{
		rb.AddForce(Vector3.up * Time.deltaTime * 1000f);
	}
}

// Game
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
	public bool playing;

	public bool done;

	public static Game Instance
	{
		get;
		private set;
	}

	private void Start()
	{
		Instance = this;
		playing = false;
	}

	public void StartGame()
	{
		playing = true;
		done = false;
		Time.timeScale = 1f;
		UIManger.Instance.StartGame();
		Timer.Instance.StartTimer();
	}

	public void RestartGame()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		Time.timeScale = 1f;
		StartGame();
	}

	public void EndGame()
	{
		playing = false;
	}

	public void NextMap()
	{
		Time.timeScale = 1f;
		int buildIndex = SceneManager.GetActiveScene().buildIndex;
		if (buildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
		{
			MainMenu();
			return;
		}
		SceneManager.LoadScene(buildIndex + 1);
		StartGame();
	}

	public void MainMenu()
	{
		playing = false;
		SceneManager.LoadScene("MainMenu");
		UIManger.Instance.GameUI(b: false);
		Time.timeScale = 1f;
	}

	public void Win()
	{
		playing = false;
		Timer.Instance.Stop();
		Time.timeScale = 0.05f;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		UIManger.Instance.WinUI(b: true);
		float timer = Timer.Instance.GetTimer();
		int num = int.Parse(SceneManager.GetActiveScene().name[0].ToString() ?? "");
		if (int.TryParse(SceneManager.GetActiveScene().name.Substring(0, 2) ?? "", out int result))
		{
			num = result;
		}
		float num2 = SaveManager.Instance.state.times[num];
		if (timer < num2 || num2 == 0f)
		{
			SaveManager.Instance.state.times[num] = timer;
			SaveManager.Instance.Save();
		}
		MonoBehaviour.print("time has been saved as: " + Timer.Instance.GetFormattedTime(timer));
		done = true;
	}
}

// GameState
using Audio;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class GameState : MonoBehaviour
{
	public GameObject ppVolume;

	public PostProcessProfile pp;

	private MotionBlur ppBlur;

	public bool graphics = true;

	public bool muted;

	public bool blur = true;

	public bool shake = true;

	public bool slowmo = true;

	private float sensitivity = 1f;

	private float volume;

	private float music;

	public float fov = 1f;

	public float cameraShake = 1f;

	public static GameState Instance
	{
		get;
		private set;
	}

	private void Start()
	{
		Instance = this;
		ppBlur = pp.GetSetting<MotionBlur>();
		graphics = SaveManager.Instance.state.graphics;
		shake = SaveManager.Instance.state.cameraShake;
		blur = SaveManager.Instance.state.motionBlur;
		slowmo = SaveManager.Instance.state.slowmo;
		muted = SaveManager.Instance.state.muted;
		sensitivity = SaveManager.Instance.state.sensitivity;
		music = SaveManager.Instance.state.music;
		volume = SaveManager.Instance.state.volume;
		fov = SaveManager.Instance.state.fov;
		UpdateSettings();
	}

	public void SetGraphics(bool b)
	{
		graphics = b;
		ppVolume.SetActive(b);
		SaveManager.Instance.state.graphics = b;
		SaveManager.Instance.Save();
	}

	public void SetBlur(bool b)
	{
		blur = b;
		if (b)
		{
			ppBlur.shutterAngle.value = 160f;
		}
		else
		{
			ppBlur.shutterAngle.value = 0f;
		}
		SaveManager.Instance.state.motionBlur = b;
		SaveManager.Instance.Save();
	}

	public void SetShake(bool b)
	{
		shake = b;
		if (b)
		{
			cameraShake = 1f;
		}
		else
		{
			cameraShake = 0f;
		}
		SaveManager.Instance.state.cameraShake = b;
		SaveManager.Instance.Save();
	}

	public void SetSlowmo(bool b)
	{
		slowmo = b;
		SaveManager.Instance.state.slowmo = b;
		SaveManager.Instance.Save();
	}

	public void SetSensitivity(float s)
	{
		float num = sensitivity = Mathf.Clamp(s, 0f, 5f);
		if ((bool)PlayerMovement.Instance)
		{
			PlayerMovement.Instance.UpdateSensitivity();
		}
		SaveManager.Instance.state.sensitivity = num;
		SaveManager.Instance.Save();
	}

	public void SetMusic(float s)
	{
		float musicVolume = music = Mathf.Clamp(s, 0f, 1f);
		if ((bool)Music.Instance)
		{
			Music.Instance.SetMusicVolume(musicVolume);
		}
		SaveManager.Instance.state.music = musicVolume;
		SaveManager.Instance.Save();
		MonoBehaviour.print("music saved as: " + music);
	}

	public void SetVolume(float s)
	{
		float num2 = AudioListener.volume = (volume = Mathf.Clamp(s, 0f, 1f));
		SaveManager.Instance.state.volume = num2;
		SaveManager.Instance.Save();
	}

	public void SetFov(float f)
	{
		float num = fov = Mathf.Clamp(f, 50f, 150f);
		if ((bool)MoveCamera.Instance)
		{
			MoveCamera.Instance.UpdateFov();
		}
		SaveManager.Instance.state.fov = num;
		SaveManager.Instance.Save();
	}

	public void SetMuted(bool b)
	{
		AudioManager.Instance.MuteSounds(b);
		muted = b;
		SaveManager.Instance.state.muted = b;
		SaveManager.Instance.Save();
	}

	private void UpdateSettings()
	{
		SetGraphics(graphics);
		SetBlur(blur);
		SetSensitivity(sensitivity);
		SetMusic(music);
		SetVolume(volume);
		SetFov(fov);
		SetShake(shake);
		SetSlowmo(slowmo);
		SetMuted(muted);
	}

	public bool GetGraphics()
	{
		return graphics;
	}

	public float GetSensitivity()
	{
		return sensitivity;
	}

	public float GetVolume()
	{
		return volume;
	}

	public float GetMusic()
	{
		return music;
	}

	public float GetFov()
	{
		return fov;
	}

	public bool GetMuted()
	{
		return muted;
	}
}

// GE_ToggleFullScreenUI
using UnityEngine;
using UnityEngine.UI;

public class GE_ToggleFullScreenUI : MonoBehaviour
{
	private int m_DefWidth;

	private int m_DefHeight;

	private void Start()
	{
		m_DefWidth = Screen.width;
		m_DefHeight = Screen.height;
		if (!Application.isEditor)
		{
			if (Application.platform == RuntimePlatform.WebGLPlayer || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.LinuxPlayer)
			{
				base.gameObject.SetActive(value: true);
			}
			else
			{
				base.gameObject.SetActive(value: false);
			}
		}
	}

	private void Update()
	{
	}

	public void OnButton_ToggleFullScreen()
	{
		if (Application.isEditor)
		{
			if (base.gameObject.activeSelf)
			{
				base.gameObject.GetComponent<Button>().interactable = false;
				foreach (Transform item in base.transform)
				{
					item.gameObject.SetActive(value: true);
				}
			}
		}
		else
		{
			Screen.fullScreen = !Screen.fullScreen;
			if (!Screen.fullScreen)
			{
				Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, fullscreen: true);
			}
			else
			{
				Screen.SetResolution(m_DefWidth, m_DefHeight, fullscreen: false);
			}
		}
	}
}

// GE_UIResponder
using UnityEngine;
using UnityEngine.UI;

public class GE_UIResponder : MonoBehaviour
{
	public string m_PackageTitle = "-";

	public string m_TargetURL = "www.unity3d.com";

	private void Start()
	{
		GameObject gameObject = GameObject.Find("Text Package Title");
		if (gameObject != null)
		{
			gameObject.GetComponent<Text>().text = m_PackageTitle;
		}
	}

	private void Update()
	{
	}

	public void OnButton_AssetName()
	{
		Application.OpenURL(m_TargetURL);
	}
}

// Glass
using EZCameraShake;
using UnityEngine;

public class Glass : MonoBehaviour
{
	public GameObject glass;

	public GameObject glassSfx;

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer != LayerMask.NameToLayer("Ground"))
		{
			UnityEngine.Object.Instantiate(glassSfx, base.transform.position, Quaternion.identity);
			glass.SetActive(value: true);
			glass.transform.parent = null;
			glass.transform.localScale = Vector3.one;
			UnityEngine.Object.Destroy(base.gameObject);
			if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
			{
				PlayerMovement.Instance.Slowmo(0.3f, 1f);
			}
			CameraShaker.Instance.ShakeOnce(5f, 3.5f, 0.3f, 0.2f);
		}
	}
}

// Grappling
using System;
using UnityEngine;

public class Grappling : MonoBehaviour
{
	private LineRenderer lr;

	public LayerMask whatIsSickoMode;

	private Transform connectedTransform;

	private SpringJoint joint;

	private Vector3 offsetPoint;

	private Vector3 endPoint;

	private Vector3 ropeVel;

	private Vector3 desiredPos;

	private float offsetMultiplier;

	private float offsetVel;

	private bool readyToUse = true;

	public static Grappling Instance
	{
		get;
		set;
	}

	private void Start()
	{
		Instance = this;
		lr = GetComponentInChildren<LineRenderer>();
		lr.positionCount = 0;
	}

	private void Update()
	{
		DrawLine();
		if (!(connectedTransform == null))
		{
			Vector2 vector = (connectedTransform.position - base.transform.position).normalized;
			Mathf.Atan2(vector.y, vector.x);
			_ = (joint == null);
		}
	}

	private void DrawLine()
	{
		if (connectedTransform == null || joint == null)
		{
			ClearLine();
			return;
		}
		desiredPos = connectedTransform.position + offsetPoint;
		endPoint = Vector3.SmoothDamp(endPoint, desiredPos, ref ropeVel, 0.03f);
		offsetMultiplier = Mathf.SmoothDamp(offsetMultiplier, 0f, ref offsetVel, 0.12f);
		int num = 100;
		lr.positionCount = num;
		Vector3 position = base.transform.position;
		lr.SetPosition(0, position);
		lr.SetPosition(num - 1, endPoint);
		float num2 = 15f;
		float num3 = 0.5f;
		for (int i = 1; i < num - 1; i++)
		{
			float num4 = (float)i / (float)num;
			float num5 = num4 * offsetMultiplier;
			float num6 = (Mathf.Sin(num5 * num2) - 0.5f) * num3 * (num5 * 2f);
			Vector3 normalized = (endPoint - position).normalized;
			float num7 = Mathf.Sin(num4 * 180f * ((float)Math.PI / 180f));
			float num8 = Mathf.Cos(offsetMultiplier * 90f * ((float)Math.PI / 180f));
			Vector3 position2 = position + (endPoint - position) / num * i + (Vector3)(num8 * num6 * Vector2.Perpendicular(normalized) + offsetMultiplier * num7 * Vector2.down);
			lr.SetPosition(i, position2);
		}
	}

	private void ClearLine()
	{
		lr.positionCount = 0;
	}

	public void Use(Vector3 attackDirection)
	{
		if (readyToUse)
		{
			ShootRope(attackDirection);
			readyToUse = false;
		}
	}

	public void StopUse()
	{
		if (!(joint == null))
		{
			MonoBehaviour.print("STOPPING");
			connectedTransform = null;
			readyToUse = true;
		}
	}

	private void ShootRope(Vector3 dir)
	{
		RaycastHit[] array = Physics.RaycastAll(base.transform.position, dir, 10f, whatIsSickoMode);
		GameObject gameObject = null;
		RaycastHit raycastHit = default(RaycastHit);
		RaycastHit[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			RaycastHit raycastHit2 = array2[i];
			gameObject = raycastHit2.collider.gameObject;
			if (gameObject.layer == LayerMask.NameToLayer("Player"))
			{
				gameObject = null;
				continue;
			}
			raycastHit = raycastHit2;
			break;
		}
		if (!(gameObject == null) && !(raycastHit.collider == null))
		{
			connectedTransform = raycastHit.collider.transform;
			joint = base.gameObject.AddComponent<SpringJoint>();
			Rigidbody component = gameObject.GetComponent<Rigidbody>();
			offsetPoint = raycastHit.point - raycastHit.collider.transform.position;
			joint.connectedBody = gameObject.GetComponent<Rigidbody>();
			if (component == null)
			{
				joint.connectedAnchor = raycastHit.point;
			}
			else
			{
				joint.connectedAnchor = offsetPoint;
			}
			joint.autoConfigureConnectedAnchor = false;
			endPoint = base.transform.position;
			offsetMultiplier = 1f;
		}
	}
}

// Grappler
using Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grappler : Pickup
{
	private Transform tip;

	private bool grappling;

	public LayerMask whatIsGround;

	private Vector3 grapplePoint;

	private SpringJoint joint;

	private LineRenderer lr;

	private Vector3 endPoint;

	private float offsetMultiplier;

	private float offsetVel;

	public GameObject aim;

	private int positions = 100;

	private Vector3 aimVel;

	private Vector3 scaleVel;

	private Vector3 nearestPoint;

	private void Start()
	{
		tip = base.transform.GetChild(0);
		lr = GetComponent<LineRenderer>();
		lr.positionCount = positions;
		aim.transform.parent = null;
		aim.SetActive(value: false);
	}

	public override void Use(Vector3 attackDirection)
	{
		if (grappling)
		{
			return;
		}
		grappling = true;
		Transform playerCamTransform = PlayerMovement.Instance.GetPlayerCamTransform();
		Transform transform = PlayerMovement.Instance.transform;
		RaycastHit[] array = Physics.RaycastAll(playerCamTransform.position, playerCamTransform.forward, 70f, whatIsGround);
		if (array.Length < 1)
		{
			if (nearestPoint == Vector3.zero)
			{
				return;
			}
			grapplePoint = nearestPoint;
		}
		else
		{
			grapplePoint = array[0].point;
		}
		joint = transform.gameObject.AddComponent<SpringJoint>();
		joint.autoConfigureConnectedAnchor = false;
		joint.connectedAnchor = grapplePoint;
		joint.maxDistance = Vector2.Distance(grapplePoint, transform.position) * 0.8f;
		joint.minDistance = Vector2.Distance(grapplePoint, transform.position) * 0.25f;
		joint.spring = 4.5f;
		joint.damper = 7f;
		joint.massScale = 4.5f;
		endPoint = tip.position;
		offsetMultiplier = 2f;
		lr.positionCount = positions;
		AudioManager.Instance.PlayPitched("Grapple", 0.2f);
	}

	public override void OnAim()
	{
		if (grappling)
		{
			return;
		}
		Transform playerCamTransform = PlayerMovement.Instance.GetPlayerCamTransform();
		List<RaycastHit> list = Physics.RaycastAll(playerCamTransform.position, playerCamTransform.forward, 70f, whatIsGround).ToList();
		if (list.Count > 0)
		{
			aim.SetActive(value: false);
			aim.transform.localScale = Vector3.zero;
			return;
		}
		int num = 50;
		int num2 = 10;
		float d = 0.035f;
		bool flag = list.Count > 0;
		for (int i = 0; i < num2; i++)
		{
			if (flag)
			{
				break;
			}
			for (int j = 0; j < num; j++)
			{
				float f = (float)Math.PI * 2f / (float)num * (float)j;
				float d2 = Mathf.Cos(f);
				float d3 = Mathf.Sin(f);
				Vector3 a = playerCamTransform.right * d2 + playerCamTransform.up * d3;
				list.AddRange(Physics.RaycastAll(playerCamTransform.position, playerCamTransform.forward + a * d * i, 70f, whatIsGround));
			}
			if (list.Count > 0)
			{
				flag = true;
				break;
			}
		}
		nearestPoint = FindNearestPoint(list);
		if (list.Count > 0 && !grappling)
		{
			aim.SetActive(value: true);
			aim.transform.position = Vector3.SmoothDamp(aim.transform.position, nearestPoint, ref aimVel, 0.05f);
			Vector3 target = 0.025f * list[0].distance * Vector3.one;
			aim.transform.localScale = Vector3.SmoothDamp(aim.transform.localScale, target, ref scaleVel, 0.2f);
		}
		else
		{
			aim.SetActive(value: false);
			aim.transform.localScale = Vector3.zero;
		}
	}

	private Vector3 FindNearestPoint(List<RaycastHit> hits)
	{
		Transform playerCamTransform = PlayerMovement.Instance.GetPlayerCamTransform();
		Vector3 result = Vector3.zero;
		float num = float.PositiveInfinity;
		for (int i = 0; i < hits.Count; i++)
		{
			if (hits[i].distance < num)
			{
				num = hits[i].distance;
				result = hits[i].collider.ClosestPoint(playerCamTransform.position + playerCamTransform.forward * num);
			}
		}
		return result;
	}

	public override void StopUse()
	{
		UnityEngine.Object.Destroy(joint);
		grapplePoint = Vector3.zero;
		grappling = false;
	}

	private void LateUpdate()
	{
		DrawGrapple();
	}

	private void DrawGrapple()
	{
		if (grapplePoint == Vector3.zero || joint == null)
		{
			lr.positionCount = 0;
			return;
		}
		endPoint = Vector3.Lerp(endPoint, grapplePoint, Time.deltaTime * 15f);
		offsetMultiplier = Mathf.SmoothDamp(offsetMultiplier, 0f, ref offsetVel, 0.1f);
		Vector3 position = tip.position;
		float num = Vector3.Distance(endPoint, position);
		lr.SetPosition(0, position);
		lr.SetPosition(positions - 1, endPoint);
		float num2 = num;
		float num3 = 1f;
		for (int i = 1; i < positions - 1; i++)
		{
			float num4 = (float)i / (float)positions;
			float num5 = num4 * offsetMultiplier;
			float num6 = (Mathf.Sin(num5 * num2) - 0.5f) * num3 * (num5 * 2f);
			Vector3 normalized = (endPoint - position).normalized;
			float num7 = Mathf.Sin(num4 * 180f * ((float)Math.PI / 180f));
			float num8 = Mathf.Cos(offsetMultiplier * 90f * ((float)Math.PI / 180f));
			Vector3 position2 = position + (endPoint - position) / positions * i + ((Vector3)(num8 * num6 * Vector2.Perpendicular(normalized)) + offsetMultiplier * num7 * Vector3.down);
			lr.SetPosition(i, position2);
		}
	}

	public Vector3 GetGrapplePoint()
	{
		return grapplePoint;
	}
}

// IPickup
using UnityEngine;

public interface IPickup
{
	void Use(Vector3 attackDirection);

	bool IsPickedUp();
}

// Gun
using UnityEngine;

public class Gun : MonoBehaviour
{
	private Vector3 rotationVel;

	private float speed = 8f;

	private float posSpeed = 0.075f;

	private float posOffset = 0.004f;

	private Vector3 defaultPos;

	private Vector3 posVel;

	private Rigidbody rb;

	private float rotationOffset;

	private float rotationOffsetZ;

	private float rotVelY;

	private float rotVelZ;

	private Vector3 prevRotation;

	private Vector3 desiredBob;

	private float xBob = 0.12f;

	private float yBob = 0.08f;

	private float zBob = 0.1f;

	private float bobSpeed = 0.45f;

	public static Gun Instance
	{
		get;
		set;
	}

	private void Start()
	{
		Instance = this;
		defaultPos = base.transform.localPosition;
		rb = PlayerMovement.Instance.GetRb();
	}

	private void Update()
	{
		if (!PlayerMovement.Instance || PlayerMovement.Instance.HasGun())
		{
			MoveGun();
			Vector3 grapplePoint = PlayerMovement.Instance.GetGrapplePoint();
			Quaternion b = Quaternion.LookRotation((PlayerMovement.Instance.GetGrapplePoint() - base.transform.position).normalized);
			rotationOffset += Mathf.DeltaAngle(base.transform.parent.rotation.eulerAngles.y, prevRotation.y);
			if (rotationOffset > 90f)
			{
				rotationOffset = 90f;
			}
			if (rotationOffset < -90f)
			{
				rotationOffset = -90f;
			}
			rotationOffset = Mathf.SmoothDamp(rotationOffset, 0f, ref rotVelY, 0.025f);
			Vector3 b2 = new Vector3(0f, rotationOffset, rotationOffset);
			if (grapplePoint == Vector3.zero)
			{
				b = Quaternion.Euler(base.transform.parent.rotation.eulerAngles - b2);
			}
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * speed);
			Vector3 vector = PlayerMovement.Instance.FindVelRelativeToLook() * posOffset;
			float num = PlayerMovement.Instance.GetFallSpeed() * posOffset;
			if (num < -0.08f)
			{
				num = -0.08f;
			}
			Vector3 a = defaultPos - new Vector3(vector.x, num, vector.y);
			base.transform.localPosition = Vector3.SmoothDamp(base.transform.localPosition, a + desiredBob, ref posVel, posSpeed);
			prevRotation = base.transform.parent.rotation.eulerAngles;
		}
	}

	private void MoveGun()
	{
		if ((bool)rb && PlayerMovement.Instance.grounded)
		{
			if (Mathf.Abs(rb.velocity.magnitude) < 4f)
			{
				desiredBob = Vector3.zero;
				return;
			}
			float x = Mathf.PingPong(Time.time * bobSpeed, xBob) - xBob / 2f;
			float y = Mathf.PingPong(Time.time * bobSpeed, yBob) - yBob / 2f;
			float z = Mathf.PingPong(Time.time * bobSpeed, zBob) - zBob / 2f;
			desiredBob = new Vector3(x, y, z);
		}
	}

	public void Shoot()
	{
		float recoil = PlayerMovement.Instance.GetRecoil();
		Vector3 b = (Vector3.up / 4f + Vector3.back / 1.5f) * recoil;
		base.transform.localPosition = base.transform.localPosition + b;
		Quaternion localRotation = Quaternion.Euler(-60f * recoil, 0f, 0f);
		base.transform.localRotation = localRotation;
	}
}

// Lava
using UnityEngine;

public class Lava : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
		{
			PlayerMovement.Instance.KillPlayer();
		}
	}
}

// Lobby
using Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Lobby : MonoBehaviour
{
	private void Start()
	{
	}

	public void LoadMap(string s)
	{
		if (!s.Equals(""))
		{
			SceneManager.LoadScene(s);
			Game.Instance.StartGame();
		}
	}

	public void Exit()
	{
		Application.Quit(0);
	}

	public void ButtonSound()
	{
		AudioManager.Instance.Play("Button");
	}

	public void Youtube()
	{
		Application.OpenURL("https://youtube.com/danidev");
	}

	public void Twitter()
	{
		Application.OpenURL("https://twitter.com/DaniDevYT");
	}

	public void Facebook()
	{
		Application.OpenURL("https://www.facebook.com/DWSgames");
	}

	public void Discord()
	{
		Application.OpenURL("https://discord.gg/P53pFtR");
	}

	public void Steam()
	{
		Application.OpenURL("https://store.steampowered.com/app/1228610/Karlson");
	}

	public void EvanYoutube()
	{
		Application.OpenURL("https://www.youtube.com/user/EvanKingAudio");
	}

	public void EvanTwitter()
	{
		Application.OpenURL("https://twitter.com/EvanKingAudio");
	}
}

// MainCamera
using UnityEngine;

public class MainCamera : MonoBehaviour
{
	private void Awake()
	{
		if ((bool)SlowmoEffect.Instance)
		{
			SlowmoEffect.Instance.NewScene(GetComponent<AudioLowPassFilter>(), GetComponent<AudioDistortionFilter>());
		}
	}
}

// Managers
using UnityEngine;
using UnityEngine.SceneManagement;

public class Managers : MonoBehaviour
{
	public static Managers Instance
	{
		get;
		private set;
	}

	private void Start()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		SceneManager.LoadScene("MainMenu");
		Time.timeScale = 1f;
		Application.targetFrameRate = 240;
		QualitySettings.vSyncCount = 0;
	}
}

// MenuCamera
using EZCameraShake;
using UnityEngine;

public class MenuCamera : MonoBehaviour
{
	private Vector3 startPos;

	private Vector3 options = new Vector3(0f, 3.6f, 8f);

	private Vector3 play = new Vector3(1f, 4.6f, 5.5f);

	private Vector3 about = new Vector3(1f, 5.5f, 5.5f);

	private Vector3 desiredPos;

	private Vector3 posVel;

	private Vector3 startRot;

	private Vector3 playRot;

	private Vector3 aboutRot;

	private Quaternion desiredRot;

	private void Start()
	{
		startPos = base.transform.position;
		desiredPos = startPos;
		options += startPos;
		play += startPos;
		about += startPos;
		CameraShaker.Instance.StartShake(1f, 0.04f, 0.1f);
		startRot = Vector3.zero;
		playRot = new Vector3(0f, 90f, 0f);
		aboutRot = new Vector3(-90f, 0f, 0f);
	}

	private void Update()
	{
		base.transform.position = Vector3.SmoothDamp(base.transform.position, desiredPos, ref posVel, 0.4f);
		base.transform.rotation = Quaternion.Slerp(base.transform.rotation, desiredRot, Time.deltaTime * 4f);
	}

	public void Options()
	{
		desiredPos = options;
	}

	public void Main()
	{
		desiredPos = startPos;
		desiredRot = Quaternion.Euler(startRot);
	}

	public void Play()
	{
		desiredPos = play;
		desiredRot = Quaternion.Euler(playRot);
	}

	public void About()
	{
		desiredPos = about;
		desiredRot = Quaternion.Euler(aboutRot);
	}
}

// Milk
using UnityEngine;

public class Milk : MonoBehaviour
{
	private void Update()
	{
		float z = Mathf.PingPong(Time.time, 1f);
		Vector3 axis = new Vector3(1f, 1f, z);
		base.transform.Rotate(axis, 0.5f);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Player") && !PlayerMovement.Instance.IsDead())
		{
			Game.Instance.Win();
			MonoBehaviour.print("Player won");
		}
	}
}

// MoveCamera
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
	public Transform player;

	private Vector3 offset;

	private Camera cam;

	public static MoveCamera Instance
	{
		get;
		private set;
	}

	private void Start()
	{
		Instance = this;
		cam = base.transform.GetChild(0).GetComponent<Camera>();
		cam.fieldOfView = GameState.Instance.fov;
		offset = base.transform.position - player.transform.position;
	}

	private void Update()
	{
		base.transform.position = player.transform.position;
	}

	public void UpdateFov()
	{
		cam.fieldOfView = GameState.Instance.fov;
	}
}

// Movement
using Audio;
using EZCameraShake;
using System;
using UnityEngine;

public class Movement : MonoBehaviour
{
	public GameObject spawnWeapon;

	private float sensitivity = 50f;

	private float sensMultiplier = 1f;

	private bool dead;

	public PhysicMaterial deadMat;

	public Transform playerCam;

	public Transform orientation;

	public Transform gun;

	private float xRotation;

	public Rigidbody rb;

	private float moveSpeed = 4500f;

	private float walkSpeed = 20f;

	private float runSpeed = 10f;

	public bool grounded;

	public Transform groundChecker;

	public LayerMask whatIsGround;

	private bool readyToJump;

	private float jumpCooldown = 0.2f;

	private float jumpForce = 550f;

	private float x;

	private float y;

	private bool jumping;

	private bool sprinting;

	private bool crouching;

	public LineRenderer lr;

	private Vector3 grapplePoint;

	private SpringJoint joint;

	private Vector3 normalVector;

	private Vector3 wallNormalVector;

	private bool wallRunning;

	private Vector3 wallRunPos;

	private DetectWeapons detectWeapons;

	public ParticleSystem ps;

	private ParticleSystem.EmissionModule psEmission;

	private Collider playerCollider;

	public bool paused;

	public LayerMask whatIsGrabbable;

	private Rigidbody objectGrabbing;

	private Vector3 previousLookdir;

	private Vector3 grabPoint;

	private float dragForce = 700000f;

	private SpringJoint grabJoint;

	private LineRenderer grabLr;

	private Vector3 myGrabPoint;

	private Vector3 myHandPoint;

	private Vector3 endPoint;

	private Vector3 grappleVel;

	private float offsetMultiplier;

	private float offsetVel;

	private float distance;

	private float actualWallRotation;

	private float wallRotationVel;

	private float desiredX;

	private float wallRunRotation;

	private bool airborne;

	private bool touchingGround;

	public LayerMask whatIsHittable;

	private float desiredTimeScale = 1f;

	private float timeScaleVel;

	public static Movement Instance
	{
		get;
		private set;
	}

	private void Awake()
	{
		Instance = this;
		rb = GetComponent<Rigidbody>();
		MonoBehaviour.print("rb: " + rb);
	}

	private void Start()
	{
		psEmission = ps.emission;
		playerCollider = GetComponent<Collider>();
		detectWeapons = (DetectWeapons)GetComponentInChildren(typeof(DetectWeapons));
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		readyToJump = true;
		wallNormalVector = Vector3.up;
		CameraShake();
		if (spawnWeapon != null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(spawnWeapon, base.transform.position, Quaternion.identity);
			detectWeapons.ForcePickup(gameObject);
		}
		if ((bool)GameState.Instance)
		{
			sensMultiplier = GameState.Instance.GetSensitivity();
		}
	}

	private void LateUpdate()
	{
		if (!dead && !paused)
		{
			DrawGrapple();
			DrawGrabbing();
			WallRunning();
		}
	}

	private void FixedUpdate()
	{
		if (!dead && !Game.Instance.done && !paused)
		{
			Move();
		}
	}

	private void Update()
	{
		MyInput();
		if (!dead && !Game.Instance.done && !paused)
		{
			Look();
			DrawGrabbing();
			UpdateTimescale();
			if (base.transform.position.y < -200f)
			{
				KillPlayer();
			}
		}
	}

	private void MyInput()
	{
		if (dead || Game.Instance.done)
		{
			return;
		}
		x = Input.GetAxisRaw("Horizontal");
		y = Input.GetAxisRaw("Vertical");
		jumping = Input.GetButton("Jump");
		sprinting = Input.GetKey(KeyCode.LeftShift);
		crouching = Input.GetKey(KeyCode.LeftControl);
		if (Input.GetKeyDown(KeyCode.LeftControl))
		{
			StartCrouch();
		}
		if (Input.GetKeyUp(KeyCode.LeftControl))
		{
			StopCrouch();
		}
		if (Input.GetKey(KeyCode.Mouse0))
		{
			if (detectWeapons.HasGun())
			{
				detectWeapons.Shoot(HitPoint());
			}
			else
			{
				GrabObject();
			}
		}
		if (Input.GetKeyUp(KeyCode.Mouse0))
		{
			detectWeapons.StopUse();
			if ((bool)objectGrabbing)
			{
				StopGrab();
			}
		}
		if (Input.GetKeyDown(KeyCode.E))
		{
			detectWeapons.Pickup();
		}
		if (Input.GetKeyDown(KeyCode.Q))
		{
			detectWeapons.Throw((HitPoint() - detectWeapons.weaponPos.position).normalized);
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Pause();
		}
	}

	private void Pause()
	{
		if (!dead)
		{
			if (paused)
			{
				Time.timeScale = 1f;
				UIManger.Instance.DeadUI(b: false);
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
				paused = false;
			}
			else
			{
				paused = true;
				Time.timeScale = 0f;
				UIManger.Instance.DeadUI(b: true);
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
	}

	private void UpdateTimescale()
	{
		if (!Game.Instance.done && !paused && !dead)
		{
			Time.timeScale = Mathf.SmoothDamp(Time.timeScale, desiredTimeScale, ref timeScaleVel, 0.15f);
		}
	}

	private void GrabObject()
	{
		if (objectGrabbing == null)
		{
			StartGrab();
		}
		else
		{
			HoldGrab();
		}
	}

	private void DrawGrabbing()
	{
		if ((bool)objectGrabbing)
		{
			myGrabPoint = Vector3.Lerp(myGrabPoint, objectGrabbing.position, Time.deltaTime * 45f);
			myHandPoint = Vector3.Lerp(myHandPoint, grabJoint.connectedAnchor, Time.deltaTime * 45f);
			grabLr.SetPosition(0, myGrabPoint);
			grabLr.SetPosition(1, myHandPoint);
		}
	}

	private void StartGrab()
	{
		RaycastHit[] array = Physics.RaycastAll(playerCam.transform.position, playerCam.transform.forward, 8f, whatIsGrabbable);
		if (array.Length < 1)
		{
			return;
		}
		int num = 0;
		while (true)
		{
			if (num < array.Length)
			{
				MonoBehaviour.print("testing on: " + array[num].collider.gameObject.layer);
				if ((bool)array[num].transform.GetComponent<Rigidbody>())
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		objectGrabbing = array[num].transform.GetComponent<Rigidbody>();
		grabPoint = array[num].point;
		grabJoint = objectGrabbing.gameObject.AddComponent<SpringJoint>();
		grabJoint.autoConfigureConnectedAnchor = false;
		grabJoint.minDistance = 0f;
		grabJoint.maxDistance = 0f;
		grabJoint.damper = 4f;
		grabJoint.spring = 40f;
		grabJoint.massScale = 5f;
		objectGrabbing.angularDrag = 5f;
		objectGrabbing.drag = 1f;
		previousLookdir = playerCam.transform.forward;
		grabLr = objectGrabbing.gameObject.AddComponent<LineRenderer>();
		grabLr.positionCount = 2;
		grabLr.startWidth = 0.05f;
		grabLr.material = new Material(Shader.Find("Sprites/Default"));
		grabLr.numCapVertices = 10;
		grabLr.numCornerVertices = 10;
	}

	private void HoldGrab()
	{
		grabJoint.connectedAnchor = playerCam.transform.position + playerCam.transform.forward * 5.5f;
		grabLr.startWidth = 0f;
		grabLr.endWidth = 0.0075f * objectGrabbing.velocity.magnitude;
		previousLookdir = playerCam.transform.forward;
	}

	private void StopGrab()
	{
		UnityEngine.Object.Destroy(grabJoint);
		UnityEngine.Object.Destroy(grabLr);
		objectGrabbing.angularDrag = 0.05f;
		objectGrabbing.drag = 0f;
		objectGrabbing = null;
	}

	private void StartCrouch()
	{
		float d = 400f;
		base.transform.localScale = new Vector3(1f, 0.5f, 1f);
		base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y - 0.5f, base.transform.position.z);
		if (rb.velocity.magnitude > 0.1f && grounded)
		{
			rb.AddForce(orientation.transform.forward * d);
			AudioManager.Instance.Play("StartSlide");
			AudioManager.Instance.Play("Slide");
		}
	}

	private void StopCrouch()
	{
		base.transform.localScale = new Vector3(1f, 1.5f, 1f);
		base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y + 0.5f, base.transform.position.z);
	}

	private void StopGrapple()
	{
		UnityEngine.Object.Destroy(joint);
		grapplePoint = Vector3.zero;
	}

	private void StartGrapple()
	{
		RaycastHit[] array = Physics.RaycastAll(playerCam.transform.position, playerCam.transform.forward, 70f, whatIsGround);
		if (array.Length >= 1)
		{
			grapplePoint = array[0].point;
			joint = base.gameObject.AddComponent<SpringJoint>();
			joint.autoConfigureConnectedAnchor = false;
			joint.connectedAnchor = grapplePoint;
			joint.spring = 6.5f;
			joint.damper = 2f;
			joint.maxDistance = Vector2.Distance(grapplePoint, base.transform.position) * 0.8f;
			joint.minDistance = Vector2.Distance(grapplePoint, base.transform.position) * 0.25f;
			joint.spring = 4.5f;
			joint.damper = 7f;
			joint.massScale = 4.5f;
			endPoint = gun.transform.GetChild(0).position;
			offsetMultiplier = 2f;
		}
	}

	private void DrawGrapple()
	{
		if (grapplePoint == Vector3.zero || joint == null)
		{
			lr.positionCount = 0;
			return;
		}
		lr.positionCount = 2;
		endPoint = Vector3.Lerp(endPoint, grapplePoint, Time.deltaTime * 15f);
		offsetMultiplier = Mathf.SmoothDamp(offsetMultiplier, 0f, ref offsetVel, 0.1f);
		int num = 100;
		lr.positionCount = num;
		Vector3 position = gun.transform.GetChild(0).position;
		float num2 = Vector3.Distance(endPoint, position);
		lr.SetPosition(0, position);
		lr.SetPosition(num - 1, endPoint);
		float num3 = num2;
		float num4 = 1f;
		for (int i = 1; i < num - 1; i++)
		{
			float num5 = (float)i / (float)num;
			float num6 = num5 * offsetMultiplier;
			float num7 = (Mathf.Sin(num6 * num3) - 0.5f) * num4 * (num6 * 2f);
			Vector3 normalized = (endPoint - position).normalized;
			float num8 = Mathf.Sin(num5 * 180f * ((float)Math.PI / 180f));
			float num9 = Mathf.Cos(offsetMultiplier * 90f * ((float)Math.PI / 180f));
			Vector3 position2 = position + (endPoint - position) / num * i + ((Vector3)(num9 * num7 * Vector2.Perpendicular(normalized)) + offsetMultiplier * num8 * Vector3.down);
			lr.SetPosition(i, position2);
		}
	}

	private void WallRunning()
	{
		if (wallRunning)
		{
			rb.AddForce(-wallNormalVector * Time.deltaTime * moveSpeed);
		}
	}

	private void FootSteps()
	{
		if (!crouching && !dead && (grounded || wallRunning))
		{
			float num = 1.2f;
			float num2 = rb.velocity.magnitude;
			if (num2 > 20f)
			{
				num2 = 20f;
			}
			distance += num2;
			if (distance > 300f / num)
			{
				AudioManager.Instance.PlayFootStep();
				distance = 0f;
			}
		}
	}

	private void Move()
	{
		if (dead)
		{
			return;
		}
		grounded = (Physics.OverlapSphere(groundChecker.position, 0.1f, whatIsGround).Length != 0);
		if (!touchingGround)
		{
			grounded = false;
		}
		Vector2 mag = FindVelRelativeToLook();
		float num = mag.x;
		float num2 = mag.y;
		FootSteps();
		CounterMovement(x, y, mag);
		if (readyToJump && jumping)
		{
			Jump();
		}
		float num3 = walkSpeed;
		if (sprinting)
		{
			num3 = runSpeed;
		}
		if (crouching && grounded && readyToJump)
		{
			rb.AddForce(Vector3.down * Time.deltaTime * 2000f);
			return;
		}
		if (x > 0f && num > num3)
		{
			x = 0f;
		}
		if (x < 0f && num < 0f - num3)
		{
			x = 0f;
		}
		if (y > 0f && num2 > num3)
		{
			y = 0f;
		}
		if (y < 0f && num2 < 0f - num3)
		{
			y = 0f;
		}
		float d = 1f;
		float d2 = 1f;
		if (!grounded)
		{
			d = 0.5f;
		}
		if (grounded && crouching)
		{
			d2 = 0f;
		}
		rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * d * d2);
		rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * d);
		SpeedLines();
	}

	private void SpeedLines()
	{
		float num = Vector3.Angle(rb.velocity, playerCam.transform.forward) * 0.15f;
		if (num < 1f)
		{
			num = 1f;
		}
		float rateOverTimeMultiplier = rb.velocity.magnitude / num;
		if (grounded && !wallRunning)
		{
			rateOverTimeMultiplier = 0f;
		}
		psEmission.rateOverTimeMultiplier = rateOverTimeMultiplier;
	}

	private void CameraShake()
	{
		float num = rb.velocity.magnitude / 9f;
		CameraShaker.Instance.ShakeOnce(num, 0.1f * num, 0.25f, 0.2f);
		Invoke("CameraShake", 0.2f);
	}

	private void ResetJump()
	{
		readyToJump = true;
		MonoBehaviour.print("reset");
	}

	private void Jump()
	{
		if (grounded || wallRunning)
		{
			Vector3 velocity = rb.velocity;
			rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
			readyToJump = false;
			rb.AddForce(Vector2.up * jumpForce * 1.5f);
			rb.AddForce(wallNormalVector * jumpForce * 0.5f);
			if (wallRunning)
			{
				rb.AddForce(wallNormalVector * jumpForce * 1.5f);
			}
			Invoke("ResetJump", jumpCooldown);
			if (wallRunning)
			{
				wallRunning = false;
			}
			AudioManager.Instance.PlayJump();
		}
	}

	private void Look()
	{
		float num = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
		float num2 = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
		Vector3 eulerAngles = playerCam.transform.localRotation.eulerAngles;
		desiredX = eulerAngles.y + num;
		xRotation -= num2;
		xRotation = Mathf.Clamp(xRotation, -90f, 90f);
		FindWallRunRotation();
		actualWallRotation = Mathf.SmoothDamp(actualWallRotation, wallRunRotation, ref wallRotationVel, 0.2f);
		playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, actualWallRotation);
		orientation.transform.localRotation = Quaternion.Euler(0f, desiredX, 0f);
	}

	private void FindWallRunRotation()
	{
		if (!wallRunning)
		{
			wallRunRotation = 0f;
			return;
		}
		_ = new Vector3(0f, playerCam.transform.rotation.y, 0f).normalized;
		new Vector3(0f, 0f, 1f);
		float num = 0f;
		float current = playerCam.transform.rotation.eulerAngles.y;
		if (Math.Abs(wallNormalVector.x - 1f) < 0.1f)
		{
			num = 90f;
		}
		else if (Math.Abs(wallNormalVector.x - -1f) < 0.1f)
		{
			num = 270f;
		}
		else if (Math.Abs(wallNormalVector.z - 1f) < 0.1f)
		{
			num = 0f;
		}
		else if (Math.Abs(wallNormalVector.z - -1f) < 0.1f)
		{
			num = 180f;
		}
		num = Vector3.SignedAngle(new Vector3(0f, 0f, 1f), wallNormalVector, Vector3.up);
		float num2 = Mathf.DeltaAngle(current, num);
		wallRunRotation = (0f - num2 / 90f) * 15f;
	}

	private void CounterMovement(float x, float y, Vector2 mag)
	{
		if (!grounded)
		{
			return;
		}
		float d = 0.2f;
		if (crouching)
		{
			rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * 0.045f);
			return;
		}
		if (Math.Abs(x) < 0.05f || (mag.x < 0f && x > 0f) || (mag.x > 0f && x < 0f))
		{
			rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * (0f - mag.x) * d);
		}
		if (Math.Abs(y) < 0.05f || (mag.y < 0f && y > 0f) || (mag.y > 0f && y < 0f))
		{
			rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * (0f - mag.y) * d);
		}
		if (Mathf.Sqrt(Mathf.Pow(rb.velocity.x, 2f) + Mathf.Pow(rb.velocity.z, 2f)) > 20f)
		{
			float num = rb.velocity.y;
			Vector3 vector = rb.velocity.normalized * 20f;
			rb.velocity = new Vector3(vector.x, num, vector.z);
		}
	}

	public Vector2 FindVelRelativeToLook()
	{
		float current = orientation.transform.eulerAngles.y;
		float target = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * 57.29578f;
		float num = Mathf.DeltaAngle(current, target);
		float num2 = 90f - num;
		float magnitude = rb.velocity.magnitude;
		return new Vector2(y: magnitude * Mathf.Cos(num * ((float)Math.PI / 180f)), x: magnitude * Mathf.Cos(num2 * ((float)Math.PI / 180f)));
	}

	private void OnCollisionEnter(Collision other)
	{
		int layer = other.gameObject.layer;
		if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
		{
			CameraShaker.Instance.ShakeOnce(5.5f * GameState.Instance.cameraShake, 1.2f, 0.2f, 0.3f);
			if (wallRunning && other.contacts[0].normal.y == -1f)
			{
				MonoBehaviour.print("ROOF");
				return;
			}
			wallNormalVector = other.contacts[0].normal;
			MonoBehaviour.print("nv: " + wallNormalVector);
			AudioManager.Instance.PlayLanding();
			if (Math.Abs(wallNormalVector.y) < 0.1f)
			{
				StartWallRun();
			}
			airborne = false;
		}
		if (layer != LayerMask.NameToLayer("Enemy") || (grounded && !crouching) || rb.velocity.magnitude < 3f)
		{
			return;
		}
		Enemy enemy = (Enemy)other.transform.root.GetComponent(typeof(Enemy));
		if ((bool)enemy && !enemy.IsDead())
		{
			UnityEngine.Object.Instantiate(PrefabManager.Instance.enemyHitAudio, other.contacts[0].point, Quaternion.identity);
			RagdollController ragdollController = (RagdollController)other.transform.root.GetComponent(typeof(RagdollController));
			if (grounded && crouching)
			{
				ragdollController.MakeRagdoll(rb.velocity * 1.2f * 34f);
			}
			else
			{
				ragdollController.MakeRagdoll(rb.velocity.normalized * 250f);
			}
			rb.AddForce(rb.velocity.normalized * 2f, ForceMode.Impulse);
			enemy.DropGun(rb.velocity.normalized * 2f);
		}
	}

	private void StartWallRun()
	{
		if (wallRunning)
		{
			MonoBehaviour.print("stopping since wallrunning");
			return;
		}
		if (touchingGround)
		{
			MonoBehaviour.print("stopping since grounded");
			return;
		}
		MonoBehaviour.print("got through");
		float d = 20f;
		wallRunning = true;
		rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
		rb.AddForce(Vector3.up * d, ForceMode.Impulse);
	}

	private void OnCollisionExit(Collision other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
		{
			if (Math.Abs(wallNormalVector.y) < 0.1f)
			{
				MonoBehaviour.print("oof");
				wallRunning = false;
				wallNormalVector = Vector3.up;
			}
			else
			{
				touchingGround = false;
			}
			airborne = true;
		}
		if (other.gameObject.layer == LayerMask.NameToLayer("Object"))
		{
			touchingGround = false;
		}
	}

	private void OnCollisionStay(Collision other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Ground") && Math.Abs(other.contacts[0].normal.y) > 0.1f)
		{
			touchingGround = true;
			wallRunning = false;
		}
		if (other.gameObject.layer == LayerMask.NameToLayer("Object"))
		{
			touchingGround = true;
		}
	}

	public Vector3 GetVelocity()
	{
		return rb.velocity;
	}

	public float GetFallSpeed()
	{
		return rb.velocity.y;
	}

	public Vector3 GetGrapplePoint()
	{
		return detectWeapons.GetGrapplerPoint();
	}

	public Collider GetPlayerCollider()
	{
		return playerCollider;
	}

	public Transform GetPlayerCamTransform()
	{
		return playerCam.transform;
	}

	public Vector3 HitPoint()
	{
		RaycastHit[] array = Physics.RaycastAll(playerCam.transform.position, playerCam.transform.forward, (int)whatIsHittable);
		if (array.Length < 1)
		{
			return playerCam.transform.position + playerCam.transform.forward * 100f;
		}
		if (array.Length > 1)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].transform.gameObject.layer == LayerMask.NameToLayer("Enemy"))
				{
					return array[i].point;
				}
			}
		}
		return array[0].point;
	}

	public float GetRecoil()
	{
		return detectWeapons.GetRecoil();
	}

	public void KillPlayer()
	{
		if (!Game.Instance.done)
		{
			CameraShaker.Instance.ShakeOnce(3f * GameState.Instance.cameraShake, 2f, 0.1f, 0.6f);
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			UIManger.Instance.DeadUI(b: true);
			Timer.Instance.Stop();
			dead = true;
			rb.freezeRotation = false;
			playerCollider.material = deadMat;
			detectWeapons.Throw(Vector3.zero);
			paused = false;
			ResetSlowmo();
		}
	}

	public void Respawn()
	{
		detectWeapons.StopUse();
	}

	public void Slowmo(float timescale, float length)
	{
		if (GameState.Instance.shake)
		{
			CancelInvoke("Slowmo");
			desiredTimeScale = timescale;
			Invoke("ResetSlowmo", length);
			AudioManager.Instance.Play("SlowmoStart");
		}
	}

	private void ResetSlowmo()
	{
		desiredTimeScale = 1f;
		AudioManager.Instance.Play("SlowmoEnd");
	}

	public bool IsCrouching()
	{
		return crouching;
	}

	public bool HasGun()
	{
		return detectWeapons.HasGun();
	}

	public bool IsDead()
	{
		return dead;
	}

	public Rigidbody GetRb()
	{
		return rb;
	}
}

// Music
using UnityEngine;

public class Music : MonoBehaviour
{
	private AudioSource music;

	private float multiplier;

	private float desiredVolume;

	private float vel;

	public static Music Instance
	{
		get;
		private set;
	}

	private void Awake()
	{
		Instance = this;
		music = GetComponent<AudioSource>();
		music.volume = 0.04f;
		multiplier = 1f;
	}

	private void Update()
	{
		desiredVolume = 0.016f * multiplier;
		if (Game.Instance.playing)
		{
			desiredVolume = 0.6f * multiplier;
		}
		music.volume = Mathf.SmoothDamp(music.volume, desiredVolume, ref vel, 0.6f);
	}

	public void SetMusicVolume(float f)
	{
		multiplier = f;
	}
}

// NavTest
using UnityEngine;
using UnityEngine.AI;

public class NavTest : MonoBehaviour
{
	private NavMeshAgent agent;

	private void Start()
	{
		agent = GetComponent<NavMeshAgent>();
	}

	private void Update()
	{
		if ((bool)PlayerMovement.Instance)
		{
			Vector3 position = PlayerMovement.Instance.transform.position;
			if (agent.isOnNavMesh)
			{
				agent.destination = position;
				MonoBehaviour.print("goin");
			}
		}
	}
}

// Object
using UnityEngine;

public class Object : MonoBehaviour
{
	private bool ready = true;

	private bool hitReady = true;

	private void OnCollisionEnter(Collision other)
	{
		float num = other.relativeVelocity.magnitude * 0.025f;
		if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") && hitReady && num > 0.8f)
		{
			hitReady = false;
			Vector3 normalized = GetComponent<Rigidbody>().velocity.normalized;
			UnityEngine.Object.Instantiate(PrefabManager.Instance.enemyHitAudio, other.contacts[0].point, Quaternion.identity);
			((RagdollController)other.transform.root.GetComponent(typeof(RagdollController))).MakeRagdoll(normalized * 350f);
			Rigidbody component = other.gameObject.GetComponent<Rigidbody>();
			if ((bool)component)
			{
				component.AddForce(normalized * 1100f);
			}
			((Enemy)other.transform.root.GetComponent(typeof(Enemy))).DropGun(Vector3.up);
		}
		if (ready)
		{
			ready = false;
			AudioSource component2 = UnityEngine.Object.Instantiate(PrefabManager.Instance.objectImpactAudio, base.transform.position, Quaternion.identity).GetComponent<AudioSource>();
			Rigidbody component3 = GetComponent<Rigidbody>();
			float num2 = 1f;
			if ((bool)component3)
			{
				num2 = component3.mass;
			}
			if (num2 < 0.3f)
			{
				num2 = 0.5f;
			}
			if (num2 > 1f)
			{
				num2 = 1f;
			}
			_ = component2.volume;
			if (num > 1f)
			{
				num = 1f;
			}
			component2.volume = num * num2;
			Invoke("GetReady", 0.1f);
		}
	}

	private void GetReady()
	{
		ready = true;
	}
}

// Options
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
	public TextMeshProUGUI sens;

	public TextMeshProUGUI volume;

	public TextMeshProUGUI music;

	public TextMeshProUGUI fov;

	public TextMeshProUGUI[] sounds;

	public TextMeshProUGUI[] graphics;

	public TextMeshProUGUI[] shake;

	public TextMeshProUGUI[] slowmo;

	public TextMeshProUGUI[] blur;

	public Slider sensS;

	public Slider volumeS;

	public Slider musicS;

	public Slider fovS;

	private void OnEnable()
	{
		UpdateList(graphics, GameState.Instance.GetGraphics());
		UpdateList(shake, GameState.Instance.shake);
		UpdateList(slowmo, GameState.Instance.slowmo);
		UpdateList(blur, GameState.Instance.blur);
		sensS.value = GameState.Instance.GetSensitivity();
		volumeS.value = GameState.Instance.GetVolume();
		musicS.value = GameState.Instance.GetMusic();
		fovS.value = GameState.Instance.GetFov();
		MonoBehaviour.print(GameState.Instance.GetMusic());
		UpdateSensitivity();
		UpdateFov();
		UpdateVolume();
		UpdateMusic();
	}

	public void ChangeGraphics(bool b)
	{
		GameState.Instance.SetGraphics(b);
		UpdateList(graphics, b);
	}

	public void ChangeBlur(bool b)
	{
		GameState.Instance.SetBlur(b);
		UpdateList(blur, b);
	}

	public void ChangeShake(bool b)
	{
		GameState.Instance.SetShake(b);
		UpdateList(shake, b);
	}

	public void ChangeSlowmo(bool b)
	{
		GameState.Instance.SetSlowmo(b);
		UpdateList(slowmo, b);
	}

	public void UpdateSensitivity()
	{
		float value = sensS.value;
		GameState.Instance.SetSensitivity(value);
		sens.text = $"{value:F2}";
	}

	public void UpdateVolume()
	{
		float num = AudioListener.volume = volumeS.value;
		GameState.Instance.SetVolume(num);
		volume.text = $"{num:F2}";
	}

	public void UpdateMusic()
	{
		float value = musicS.value;
		GameState.Instance.SetMusic(value);
		music.text = $"{value:F2}";
	}

	public void UpdateFov()
	{
		float value = fovS.value;
		GameState.Instance.SetFov(value);
		fov.text = string.Concat(value);
	}

	private void UpdateList(TextMeshProUGUI[] list, bool b)
	{
		if (!b)
		{
			list[1].color = Color.white;
			list[0].color = (Color.clear + Color.white) / 2f;
		}
		else
		{
			list[1].color = (Color.clear + Color.white) / 2f;
			list[0].color = Color.white;
		}
	}
}

// Pickup
using UnityEngine;

public abstract class Pickup : MonoBehaviour, IPickup
{
	protected bool player;

	private bool thrown;

	public float recoil;

	private Transform outline;

	public bool pickedUp
	{
		get;
		set;
	}

	public bool readyToUse
	{
		get;
		set;
	}

	private void Awake()
	{
		readyToUse = true;
		outline = base.transform.GetChild(1);
	}

	private void Update()
	{
		_ = pickedUp;
	}

	public void PickupWeapon(bool player)
	{
		pickedUp = true;
		this.player = player;
		outline.gameObject.SetActive(value: false);
	}

	public void Drop()
	{
		readyToUse = true;
		Invoke("DropWeapon", 0.5f);
		thrown = true;
	}

	private void DropWeapon()
	{
		CancelInvoke();
		pickedUp = false;
		outline.gameObject.SetActive(value: true);
	}

	public abstract void Use(Vector3 attackDirection);

	public abstract void OnAim();

	public abstract void StopUse();

	public bool IsPickedUp()
	{
		return pickedUp;
	}

	private void OnCollisionEnter(Collision other)
	{
		if (!thrown)
		{
			return;
		}
		if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
		{
			UnityEngine.Object.Instantiate(PrefabManager.Instance.enemyHitAudio, other.contacts[0].point, Quaternion.identity);
			((RagdollController)other.transform.root.GetComponent(typeof(RagdollController))).MakeRagdoll(-base.transform.right * 60f);
			Rigidbody component = other.gameObject.GetComponent<Rigidbody>();
			if ((bool)component)
			{
				component.AddForce(-base.transform.right * 1500f);
			}
			((Enemy)other.transform.root.GetComponent(typeof(Enemy))).DropGun(Vector3.up);
		}
		thrown = false;
	}
}

// Weapon
using UnityEngine;

public abstract class Weapon : Pickup
{
	public float attackSpeed;

	public float damage;

	public TrailRenderer trailRenderer;

	public float MultiplierDamage
	{
		get;
		set;
	}

	public void Start()
	{
		MultiplierDamage = 1f;
	}

	protected void Cooldown()
	{
		base.readyToUse = true;
	}

	public float GetAttackSpeed()
	{
		return attackSpeed;
	}
}

// UIManger
using UnityEngine;

public class UIManger : MonoBehaviour
{
	public GameObject gameUI;

	public GameObject deadUI;

	public GameObject winUI;

	public static UIManger Instance
	{
		get;
		private set;
	}

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		gameUI.SetActive(value: false);
	}

	public void StartGame()
	{
		gameUI.SetActive(value: true);
		DeadUI(b: false);
		WinUI(b: false);
	}

	public void GameUI(bool b)
	{
		gameUI.SetActive(b);
	}

	public void DeadUI(bool b)
	{
		deadUI.SetActive(b);
	}

	public void WinUI(bool b)
	{
		winUI.SetActive(b);
		MonoBehaviour.print("setting win UI");
	}
}

// Timer
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
	private TextMeshProUGUI text;

	private float timer;

	private bool stop;

	public static Timer Instance
	{
		get;
		set;
	}

	private void Awake()
	{
		Instance = this;
		text = GetComponent<TextMeshProUGUI>();
		stop = false;
	}

	public void StartTimer()
	{
		stop = false;
		timer = 0f;
	}

	private void Update()
	{
		if (Game.Instance.playing && !stop)
		{
			timer += Time.deltaTime;
			text.text = GetFormattedTime(timer);
		}
	}

	public string GetFormattedTime(float f)
	{
		if (f == 0f)
		{
			return "nan";
		}
		string arg = Mathf.Floor(f / 60f).ToString("00");
		string arg2 = Mathf.Floor(f % 60f).ToString("00");
		string text = (f * 100f % 100f).ToString("00");
		if (text.Equals("100"))
		{
			text = "99";
		}
		return $"{arg}:{arg2}:{text}";
	}

	public float GetTimer()
	{
		return timer;
	}

	private string StatusText(float f)
	{
		if (f < 2f)
		{
			return "very easy";
		}
		if (f < 4f)
		{
			return "easy";
		}
		if (f < 8f)
		{
			return "medium";
		}
		if (f < 12f)
		{
			return "hard";
		}
		if (f < 16f)
		{
			return "very hard";
		}
		if (f < 20f)
		{
			return "impossible";
		}
		if (f < 25f)
		{
			return "oh shit";
		}
		if (f < 30f)
		{
			return "very oh shit";
		}
		return "f";
	}

	public void Stop()
	{
		stop = true;
	}

	public int GetMinutes()
	{
		return (int)Mathf.Floor(timer / 60f);
	}
}

// TextureScaling
using UnityEngine;

[ExecuteInEditMode]
public class TextureScaling : MonoBehaviour
{
	private Vector3 _currentScale;

	public float size = 1f;

	private void Start()
	{
		Calculate();
	}

	private void Update()
	{
		Calculate();
	}

	public void Calculate()
	{
		if (!(_currentScale == base.transform.localScale) && !CheckForDefaultSize())
		{
			_currentScale = base.transform.localScale;
			Mesh mesh = GetMesh();
			mesh.uv = SetupUvMap(mesh.uv);
			mesh.name = "Cube Instance";
			if (GetComponent<Renderer>().sharedMaterial.mainTexture.wrapMode != 0)
			{
				GetComponent<Renderer>().sharedMaterial.mainTexture.wrapMode = TextureWrapMode.Repeat;
			}
		}
	}

	private Mesh GetMesh()
	{
		return GetComponent<MeshFilter>().mesh;
	}

	private Vector2[] SetupUvMap(Vector2[] meshUVs)
	{
		float x = _currentScale.x * size;
		float num = _currentScale.z * size;
		float y = _currentScale.y * size;
		meshUVs[2] = new Vector2(0f, y);
		meshUVs[3] = new Vector2(x, y);
		meshUVs[0] = new Vector2(0f, 0f);
		meshUVs[1] = new Vector2(x, 0f);
		meshUVs[7] = new Vector2(0f, 0f);
		meshUVs[6] = new Vector2(x, 0f);
		meshUVs[11] = new Vector2(0f, y);
		meshUVs[10] = new Vector2(x, y);
		meshUVs[19] = new Vector2(num, 0f);
		meshUVs[17] = new Vector2(0f, y);
		meshUVs[16] = new Vector2(0f, 0f);
		meshUVs[18] = new Vector2(num, y);
		meshUVs[23] = new Vector2(num, 0f);
		meshUVs[21] = new Vector2(0f, y);
		meshUVs[20] = new Vector2(0f, 0f);
		meshUVs[22] = new Vector2(num, y);
		meshUVs[4] = new Vector2(x, 0f);
		meshUVs[5] = new Vector2(0f, 0f);
		meshUVs[8] = new Vector2(x, num);
		meshUVs[9] = new Vector2(0f, num);
		meshUVs[13] = new Vector2(x, 0f);
		meshUVs[14] = new Vector2(0f, 0f);
		meshUVs[12] = new Vector2(x, num);
		meshUVs[15] = new Vector2(0f, num);
		return meshUVs;
	}

	private bool CheckForDefaultSize()
	{
		if (_currentScale != Vector3.one)
		{
			return false;
		}
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		UnityEngine.Object.DestroyImmediate(GetComponent<MeshFilter>());
		base.gameObject.AddComponent<MeshFilter>();
		GetComponent<MeshFilter>().sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
		UnityEngine.Object.DestroyImmediate(gameObject);
		return true;
	}
}

// SlowmoEffect
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class SlowmoEffect : MonoBehaviour
{
	public Image blackFx;

	public PostProcessProfile pp;

	private ColorGrading cg;

	private float frequency;

	private float vel;

	private float hue;

	private float hueVel;

	private AudioDistortionFilter af;

	private AudioLowPassFilter lf;

	public static SlowmoEffect Instance
	{
		get;
		private set;
	}

	private void Start()
	{
		cg = pp.GetSetting<ColorGrading>();
		Instance = this;
	}

	private void Update()
	{
		if (!af || !lf)
		{
			return;
		}
		if (!Game.Instance.playing || !Camera.main)
		{
			if (cg.hueShift.value != 0f)
			{
				cg.hueShift.value = 0f;
			}
			return;
		}
		float timeScale = Time.timeScale;
		float num = (1f - timeScale) * 2f;
		if ((double)num > 0.7)
		{
			num = 0.7f;
		}
		blackFx.color = new Color(1f, 1f, 1f, num);
		float target = PlayerMovement.Instance.GetActionMeter();
		float target2 = 0f;
		if (timeScale < 0.9f)
		{
			target = 400f;
			target2 = -20f;
		}
		frequency = Mathf.SmoothDamp(frequency, target, ref vel, 0.1f);
		hue = Mathf.SmoothDamp(hue, target2, ref hueVel, 0.2f);
		if ((bool)af)
		{
			af.distortionLevel = num * 0.2f;
		}
		if ((bool)lf)
		{
			lf.cutoffFrequency = frequency;
		}
		if ((bool)cg)
		{
			cg.hueShift.value = hue;
		}
		if (!Game.Instance.playing)
		{
			cg.hueShift.value = 0f;
		}
	}

	public void NewScene(AudioLowPassFilter l, AudioDistortionFilter d)
	{
		lf = l;
		af = d;
	}
}

// StartPlayer
using UnityEngine;

public class StartPlayer : MonoBehaviour
{
	private void Awake()
	{
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			MonoBehaviour.print("removing child: " + num);
			base.transform.GetChild(num).parent = null;
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}
}

// ShakeOnTrigger
using EZCameraShake;
using UnityEngine;

public class ShakeOnTrigger : MonoBehaviour
{
	private CameraShakeInstance _shakeInstance;

	private void Start()
	{
		_shakeInstance = CameraShaker.Instance.StartShake(2f, 15f, 2f);
		_shakeInstance.StartFadeOut(0f);
		_shakeInstance.DeleteOnInactive = true;
	}

	private void OnTriggerEnter(Collider c)
	{
		if (c.CompareTag("Player"))
		{
			_shakeInstance.StartFadeIn(1f);
		}
	}

	private void OnTriggerExit(Collider c)
	{
		if (c.CompareTag("Player"))
		{
			_shakeInstance.StartFadeOut(3f);
		}
	}
}

// ShakeOnKeyPress
using EZCameraShake;
using UnityEngine;

public class ShakeOnKeyPress : MonoBehaviour
{
	public float Magnitude = 2f;

	public float Roughness = 10f;

	public float FadeOutTime = 5f;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.LeftShift))
		{
			CameraShaker.Instance.ShakeOnce(Magnitude, Roughness, 0f, FadeOutTime);
		}
	}
}

// SaveManager
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
	public PlayerSave state;

	public static SaveManager Instance
	{
		get;
		set;
	}

	private void Awake()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		Instance = this;
		Load();
	}

	public void Save()
	{
		PlayerPrefs.SetString("save", Serialize(state));
	}

	public void Load()
	{
		if (PlayerPrefs.HasKey("save"))
		{
			state = Deserialize<PlayerSave>(PlayerPrefs.GetString("save"));
		}
		else
		{
			NewSave();
		}
	}

	public void NewSave()
	{
		state = new PlayerSave();
		Save();
		MonoBehaviour.print("Creating new save file");
	}

	public string Serialize<T>(T toSerialize)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
		StringWriter stringWriter = new StringWriter();
		xmlSerializer.Serialize(stringWriter, toSerialize);
		return stringWriter.ToString();
	}

	public T Deserialize<T>(string toDeserialize)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
		StringReader textReader = new StringReader(toDeserialize);
		return (T)xmlSerializer.Deserialize(textReader);
	}
}

// ShakeByDistance
using EZCameraShake;
using UnityEngine;

public class ShakeByDistance : MonoBehaviour
{
	public GameObject Player;

	public float Distance = 10f;

	private CameraShakeInstance _shakeInstance;

	private void Start()
	{
		_shakeInstance = CameraShaker.Instance.StartShake(2f, 14f, 0f);
	}

	private void Update()
	{
		float num = Vector3.Distance(Player.transform.position, base.transform.position);
		_shakeInstance.ScaleMagnitude = 1f - Mathf.Clamp01(num / Distance);
	}
}

// Respawn
using UnityEngine;

public class Respawn : MonoBehaviour
{
	public Transform respawnPoint;

	private void OnTriggerEnter(Collider other)
	{
		MonoBehaviour.print(other.gameObject.layer);
		if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
		{
			Transform root = other.transform.root;
			root.transform.position = respawnPoint.position;
			root.GetComponent<Rigidbody>().velocity = Vector3.zero;
		}
	}
}

// RotateObject
using UnityEngine;

public class RotateObject : MonoBehaviour
{
	private void Update()
	{
		base.transform.Rotate(Vector3.right, 40f * Time.deltaTime);
	}
}

// RangedWeapon
using Audio;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : Weapon
{
	public GameObject projectile;

	public float pushBackForce;

	public float force;

	public float accuracy;

	public int bullets;

	public float boostRecoil;

	private Transform guntip;

	private Rigidbody rb;

	private Collider[] projectileColliders;

	private new void Start()
	{
		base.Start();
		rb = GetComponent<Rigidbody>();
		guntip = base.transform.GetChild(0);
	}

	public override void Use(Vector3 attackDirection)
	{
		if (base.readyToUse && base.pickedUp)
		{
			SpawnProjectile(attackDirection);
			Recoil();
			base.readyToUse = false;
			Invoke("GetReady", attackSpeed);
		}
	}

	public override void OnAim()
	{
	}

	public override void StopUse()
	{
	}

	private void SpawnProjectile(Vector3 attackDirection)
	{
		Vector3 vector = guntip.position - guntip.transform.right / 4f;
		Vector3 normalized = (attackDirection - vector).normalized;
		List<Collider> list = new List<Collider>();
		if (player)
		{
			PlayerMovement.Instance.GetRb().AddForce(base.transform.right * boostRecoil, ForceMode.Impulse);
		}
		for (int i = 0; i < bullets; i++)
		{
			UnityEngine.Object.Instantiate(PrefabManager.Instance.muzzle, vector, Quaternion.identity);
			GameObject gameObject = UnityEngine.Object.Instantiate(projectile, vector, base.transform.rotation);
			Rigidbody componentInChildren = gameObject.GetComponentInChildren<Rigidbody>();
			projectileColliders = gameObject.GetComponentsInChildren<Collider>();
			RemoveCollisionWithPlayer();
			componentInChildren.transform.rotation = base.transform.rotation;
			Vector3 a = normalized + (guntip.transform.up * Random.Range(0f - accuracy, accuracy) + guntip.transform.forward * Random.Range(0f - accuracy, accuracy));
			componentInChildren.AddForce(componentInChildren.mass * force * a);
			Bullet bullet = (Bullet)gameObject.GetComponent(typeof(Bullet));
			if (bullet != null)
			{
				Color col = Color.red;
				if (player)
				{
					col = Color.blue;
					Gun.Instance.Shoot();
					if (bullet.explosive)
					{
						UnityEngine.Object.Instantiate(PrefabManager.Instance.thumpAudio, base.transform.position, Quaternion.identity);
					}
					else
					{
						AudioManager.Instance.PlayPitched("GunBass", 0.3f);
						AudioManager.Instance.PlayPitched("GunHigh", 0.3f);
						AudioManager.Instance.PlayPitched("GunLow", 0.3f);
					}
					componentInChildren.AddForce(componentInChildren.mass * force * a);
				}
				else
				{
					UnityEngine.Object.Instantiate(PrefabManager.Instance.gunShotAudio, base.transform.position, Quaternion.identity);
				}
				bullet.SetBullet(damage, pushBackForce, col);
				bullet.player = player;
			}
			foreach (Collider item in list)
			{
				Physics.IgnoreCollision(item, projectileColliders[0]);
			}
			list.Add(projectileColliders[0]);
		}
	}

	private void GetReady()
	{
		base.readyToUse = true;
	}

	private void Recoil()
	{
	}

	private void RemoveCollisionWithPlayer()
	{
		Collider[] array = (!player) ? base.transform.root.GetComponentsInChildren<Collider>() : new Collider[1]
		{
			PlayerMovement.Instance.GetPlayerCollider()
		};
		for (int i = 0; i < array.Length; i++)
		{
			for (int j = 0; j < projectileColliders.Length; j++)
			{
				Physics.IgnoreCollision(array[i], projectileColliders[j], ignore: true);
			}
		}
	}
}

// RandomSfx
using UnityEngine;

public class RandomSfx : MonoBehaviour
{
	public AudioClip[] sounds;

	private void Awake()
	{
		AudioSource component = GetComponent<AudioSource>();
		component.clip = sounds[Random.Range(0, sounds.Length - 1)];
		component.playOnAwake = true;
		component.pitch = 1f + Random.Range(-0.3f, 0.1f);
		component.enabled = true;
	}
}

// RagdollController
using UnityEngine;
using UnityEngine.AI;

public class RagdollController : MonoBehaviour
{
	private CharacterJoint[] c;

	private Vector3[] axis;

	private Vector3[] anchor;

	private Vector3[] swingAxis;

	public GameObject hips;

	private float[] mass;

	public GameObject[] limbs;

	private bool isRagdoll;

	public Transform leftArm;

	public Transform rightArm;

	public Transform head;

	public Transform hand;

	public Transform hand2;

	private void Start()
	{
		MakeStatic();
	}

	private void LateUpdate()
	{
	}

	public void MakeRagdoll(Vector3 dir)
	{
		if (!isRagdoll)
		{
			UnityEngine.Object.Destroy(GetComponent<NavMeshAgent>());
			UnityEngine.Object.Destroy(GetComponent("NavTest"));
			isRagdoll = true;
			UnityEngine.Object.Destroy(GetComponent<Rigidbody>());
			GetComponentInChildren<Animator>().enabled = false;
			for (int i = 0; i < limbs.Length; i++)
			{
				AddRigid(i, dir);
				limbs[i].gameObject.layer = LayerMask.NameToLayer("Object");
				limbs[i].AddComponent(typeof(Object));
			}
		}
	}

	private void AddRigid(int i, Vector3 dir)
	{
		GameObject gameObject = limbs[i];
		Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
		rigidbody.mass = mass[i];
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		rigidbody.AddForce(dir);
		if (i != 0)
		{
			CharacterJoint characterJoint = gameObject.AddComponent<CharacterJoint>();
			characterJoint.autoConfigureConnectedAnchor = true;
			characterJoint.connectedBody = FindConnectedBody(i);
			characterJoint.axis = axis[i];
			characterJoint.anchor = anchor[i];
			characterJoint.swingAxis = swingAxis[i];
		}
	}

	private Rigidbody FindConnectedBody(int i)
	{
		int num = 0;
		if (i == 2)
		{
			num = 1;
		}
		if (i == 4)
		{
			num = 3;
		}
		if (i == 7)
		{
			num = 6;
		}
		if (i == 9)
		{
			num = 8;
		}
		if (i == 10)
		{
			num = 5;
		}
		return limbs[num].GetComponent<Rigidbody>();
	}

	private void MakeStatic()
	{
		int num = limbs.Length;
		c = new CharacterJoint[num];
		Rigidbody[] array = new Rigidbody[num];
		mass = new float[num];
		for (int i = 0; i < limbs.Length; i++)
		{
			array[i] = limbs[i].GetComponent<Rigidbody>();
			mass[i] = array[i].mass;
			c[i] = limbs[i].GetComponent<CharacterJoint>();
		}
		axis = new Vector3[num];
		anchor = new Vector3[num];
		swingAxis = new Vector3[num];
		for (int j = 0; j < c.Length; j++)
		{
			if (!(c[j] == null))
			{
				axis[j] = c[j].axis;
				anchor[j] = c[j].anchor;
				swingAxis[j] = c[j].swingAxis;
				UnityEngine.Object.Destroy(c[j]);
			}
		}
		Rigidbody[] array2 = array;
		for (int k = 0; k < array2.Length; k++)
		{
			UnityEngine.Object.Destroy(array2[k]);
		}
	}

	public bool IsRagdoll()
	{
		return isRagdoll;
	}
}

// PlayUI
using TMPro;
using UnityEngine;

public class PlayUI : MonoBehaviour
{
	public TextMeshProUGUI[] maps;

	private void Start()
	{
		float[] times = SaveManager.Instance.state.times;
		for (int i = 0; i < maps.Length; i++)
		{
			MonoBehaviour.print("i: " + times[i]);
			maps[i].text = Timer.Instance.GetFormattedTime(times[i]);
		}
	}
}

// PrefabManager
using UnityEngine;

public class PrefabManager : MonoBehaviour
{
	public GameObject blood;

	public GameObject bulletDestroy;

	public GameObject muzzle;

	public GameObject explosion;

	public GameObject bulletHitAudio;

	public GameObject enemyHitAudio;

	public GameObject gunShotAudio;

	public GameObject objectImpactAudio;

	public GameObject thumpAudio;

	public GameObject destructionAudio;

	public static PrefabManager Instance
	{
		get;
		private set;
	}

	private void Awake()
	{
		Instance = this;
	}
}

// PlayerScript
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
	public void DamagePlayer(float damage)
	{
	}

	public Color GetColor()
	{
		return Color.red;
	}

	public bool IsPlayer()
	{
		return true;
	}
}

// PlayerSave
public class PlayerSave
{
	public float[] times = new float[100];

	public bool cameraShake
	{
		get;
		set;
	} = true;


	public bool motionBlur
	{
		get;
		set;
	} = true;


	public bool slowmo
	{
		get;
		set;
	} = true;


	public bool graphics
	{
		get;
		set;
	} = true;


	public bool muted
	{
		get;
		set;
	}

	public float sensitivity
	{
		get;
		set;
	} = 1f;


	public float fov
	{
		get;
		set;
	} = 80f;


	public float volume
	{
		get;
		set;
	} = 0.75f;


	public float music
	{
		get;
		set;
	} = 0.5f;

}

// PlayerMovement
using Audio;
using EZCameraShake;
using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public GameObject spawnWeapon;

	private float sensitivity = 50f;

	private float sensMultiplier = 1f;

	private bool dead;

	public PhysicMaterial deadMat;

	public Transform playerCam;

	public Transform orientation;

	public Transform gun;

	private float xRotation;

	public Rigidbody rb;

	private float moveSpeed = 4500f;

	private float walkSpeed = 20f;

	private float runSpeed = 10f;

	public bool grounded;

	public Transform groundChecker;

	public LayerMask whatIsGround;

	public LayerMask whatIsWallrunnable;

	private bool readyToJump;

	private float jumpCooldown = 0.25f;

	private float jumpForce = 550f;

	private float x;

	private float y;

	private bool jumping;

	private bool sprinting;

	private bool crouching;

	public LineRenderer lr;

	private Vector3 grapplePoint;

	private SpringJoint joint;

	private Vector3 normalVector;

	private Vector3 wallNormalVector;

	private bool wallRunning;

	private Vector3 wallRunPos;

	private DetectWeapons detectWeapons;

	public ParticleSystem ps;

	private ParticleSystem.EmissionModule psEmission;

	private Collider playerCollider;

	public bool exploded;

	public bool paused;

	public LayerMask whatIsGrabbable;

	private Rigidbody objectGrabbing;

	private Vector3 previousLookdir;

	private Vector3 grabPoint;

	private float dragForce = 700000f;

	private SpringJoint grabJoint;

	private LineRenderer grabLr;

	private Vector3 myGrabPoint;

	private Vector3 myHandPoint;

	private Vector3 endPoint;

	private Vector3 grappleVel;

	private float offsetMultiplier;

	private float offsetVel;

	private float distance;

	private float slideSlowdown = 0.2f;

	private float actualWallRotation;

	private float wallRotationVel;

	private float desiredX;

	private bool cancelling;

	private bool readyToWallrun = true;

	private float wallRunGravity = 1f;

	private float maxSlopeAngle = 35f;

	private float wallRunRotation;

	private bool airborne;

	private int nw;

	private bool onWall;

	private bool onGround;

	private bool surfing;

	private bool cancellingGrounded;

	private bool cancellingWall;

	private bool cancellingSurf;

	public LayerMask whatIsHittable;

	private float desiredTimeScale = 1f;

	private float timeScaleVel;

	private float actionMeter;

	private float vel;

	public static PlayerMovement Instance
	{
		get;
		private set;
	}

	private void Awake()
	{
		Instance = this;
		rb = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		psEmission = ps.emission;
		playerCollider = GetComponent<Collider>();
		detectWeapons = (DetectWeapons)GetComponentInChildren(typeof(DetectWeapons));
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		readyToJump = true;
		wallNormalVector = Vector3.up;
		CameraShake();
		if (spawnWeapon != null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(spawnWeapon, base.transform.position, Quaternion.identity);
			detectWeapons.ForcePickup(gameObject);
		}
		UpdateSensitivity();
	}

	public void UpdateSensitivity()
	{
		if ((bool)GameState.Instance)
		{
			sensMultiplier = GameState.Instance.GetSensitivity();
		}
	}

	private void LateUpdate()
	{
		if (!dead && !paused)
		{
			DrawGrapple();
			DrawGrabbing();
			WallRunning();
		}
	}

	private void FixedUpdate()
	{
		if (!dead && !Game.Instance.done && !paused)
		{
			Movement();
		}
	}

	private void Update()
	{
		UpdateActionMeter();
		MyInput();
		if (!dead && !Game.Instance.done && !paused)
		{
			Look();
			DrawGrabbing();
			UpdateTimescale();
			if (base.transform.position.y < -200f)
			{
				KillPlayer();
			}
		}
	}

	private void MyInput()
	{
		if (dead || Game.Instance.done)
		{
			return;
		}
		x = Input.GetAxisRaw("Horizontal");
		y = Input.GetAxisRaw("Vertical");
		jumping = Input.GetButton("Jump");
		crouching = Input.GetButton("Crouch");
		if (Input.GetButtonDown("Cancel"))
		{
			Pause();
		}
		if (paused)
		{
			return;
		}
		if (Input.GetButtonDown("Crouch"))
		{
			StartCrouch();
		}
		if (Input.GetButtonUp("Crouch"))
		{
			StopCrouch();
		}
		if (Input.GetButton("Fire1"))
		{
			if (detectWeapons.HasGun())
			{
				detectWeapons.Shoot(HitPoint());
			}
			else
			{
				GrabObject();
			}
		}
		if (Input.GetButtonUp("Fire1"))
		{
			detectWeapons.StopUse();
			if ((bool)objectGrabbing)
			{
				StopGrab();
			}
		}
		if (Input.GetButtonDown("Pickup"))
		{
			detectWeapons.Pickup();
		}
		if (Input.GetButtonDown("Drop"))
		{
			detectWeapons.Throw((HitPoint() - detectWeapons.weaponPos.position).normalized);
		}
	}

	private void Pause()
	{
		if (!dead)
		{
			if (paused)
			{
				Time.timeScale = 1f;
				UIManger.Instance.DeadUI(b: false);
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
				paused = false;
			}
			else
			{
				paused = true;
				Time.timeScale = 0f;
				UIManger.Instance.DeadUI(b: true);
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
	}

	private void UpdateTimescale()
	{
		if (!Game.Instance.done && !paused && !dead)
		{
			Time.timeScale = Mathf.SmoothDamp(Time.timeScale, desiredTimeScale, ref timeScaleVel, 0.15f);
		}
	}

	private void GrabObject()
	{
		if (objectGrabbing == null)
		{
			StartGrab();
		}
		else
		{
			HoldGrab();
		}
	}

	private void DrawGrabbing()
	{
		if ((bool)objectGrabbing)
		{
			myGrabPoint = Vector3.Lerp(myGrabPoint, objectGrabbing.position, Time.deltaTime * 45f);
			myHandPoint = Vector3.Lerp(myHandPoint, grabJoint.connectedAnchor, Time.deltaTime * 45f);
			grabLr.SetPosition(0, myGrabPoint);
			grabLr.SetPosition(1, myHandPoint);
		}
	}

	private void StartGrab()
	{
		RaycastHit[] array = Physics.RaycastAll(playerCam.transform.position, playerCam.transform.forward, 8f, whatIsGrabbable);
		if (array.Length < 1)
		{
			return;
		}
		int num = 0;
		while (true)
		{
			if (num < array.Length)
			{
				MonoBehaviour.print("testing on: " + array[num].collider.gameObject.layer);
				if ((bool)array[num].transform.GetComponent<Rigidbody>())
				{
					break;
				}
				num++;
				continue;
			}
			return;
		}
		objectGrabbing = array[num].transform.GetComponent<Rigidbody>();
		grabPoint = array[num].point;
		grabJoint = objectGrabbing.gameObject.AddComponent<SpringJoint>();
		grabJoint.autoConfigureConnectedAnchor = false;
		grabJoint.minDistance = 0f;
		grabJoint.maxDistance = 0f;
		grabJoint.damper = 4f;
		grabJoint.spring = 40f;
		grabJoint.massScale = 5f;
		objectGrabbing.angularDrag = 5f;
		objectGrabbing.drag = 1f;
		previousLookdir = playerCam.transform.forward;
		grabLr = objectGrabbing.gameObject.AddComponent<LineRenderer>();
		grabLr.positionCount = 2;
		grabLr.startWidth = 0.05f;
		grabLr.material = new Material(Shader.Find("Sprites/Default"));
		grabLr.numCapVertices = 10;
		grabLr.numCornerVertices = 10;
	}

	private void HoldGrab()
	{
		grabJoint.connectedAnchor = playerCam.transform.position + playerCam.transform.forward * 5.5f;
		grabLr.startWidth = 0f;
		grabLr.endWidth = 0.0075f * objectGrabbing.velocity.magnitude;
		previousLookdir = playerCam.transform.forward;
	}

	private void StopGrab()
	{
		UnityEngine.Object.Destroy(grabJoint);
		UnityEngine.Object.Destroy(grabLr);
		objectGrabbing.angularDrag = 0.05f;
		objectGrabbing.drag = 0f;
		objectGrabbing = null;
	}

	private void StartCrouch()
	{
		float d = 400f;
		base.transform.localScale = new Vector3(1f, 0.5f, 1f);
		base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y - 0.5f, base.transform.position.z);
		if (rb.velocity.magnitude > 0.1f && grounded)
		{
			rb.AddForce(orientation.transform.forward * d);
			AudioManager.Instance.Play("StartSlide");
			AudioManager.Instance.Play("Slide");
		}
	}

	private void StopCrouch()
	{
		base.transform.localScale = new Vector3(1f, 1.5f, 1f);
		base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y + 0.5f, base.transform.position.z);
	}

	private void DrawGrapple()
	{
		if (grapplePoint == Vector3.zero || joint == null)
		{
			lr.positionCount = 0;
			return;
		}
		lr.positionCount = 2;
		endPoint = Vector3.Lerp(endPoint, grapplePoint, Time.deltaTime * 15f);
		offsetMultiplier = Mathf.SmoothDamp(offsetMultiplier, 0f, ref offsetVel, 0.1f);
		int num = 100;
		lr.positionCount = num;
		Vector3 position = gun.transform.GetChild(0).position;
		float num2 = Vector3.Distance(endPoint, position);
		lr.SetPosition(0, position);
		lr.SetPosition(num - 1, endPoint);
		float num3 = num2;
		float num4 = 1f;
		for (int i = 1; i < num - 1; i++)
		{
			float num5 = (float)i / (float)num;
			float num6 = num5 * offsetMultiplier;
			float num7 = (Mathf.Sin(num6 * num3) - 0.5f) * num4 * (num6 * 2f);
			Vector3 normalized = (endPoint - position).normalized;
			float num8 = Mathf.Sin(num5 * 180f * ((float)Math.PI / 180f));
			float num9 = Mathf.Cos(offsetMultiplier * 90f * ((float)Math.PI / 180f));
			Vector3 position2 = position + (endPoint - position) / num * i + ((Vector3)(num9 * num7 * Vector2.Perpendicular(normalized)) + offsetMultiplier * num8 * Vector3.down);
			lr.SetPosition(i, position2);
		}
	}

	private void FootSteps()
	{
		if (!crouching && !dead && (grounded || wallRunning))
		{
			float num = 1.2f;
			float num2 = rb.velocity.magnitude;
			if (num2 > 20f)
			{
				num2 = 20f;
			}
			distance += num2;
			if (distance > 300f / num)
			{
				AudioManager.Instance.PlayFootStep();
				distance = 0f;
			}
		}
	}

	private void Movement()
	{
		if (dead)
		{
			return;
		}
		rb.AddForce(Vector3.down * Time.deltaTime * 10f);
		Vector2 mag = FindVelRelativeToLook();
		float num = mag.x;
		float num2 = mag.y;
		FootSteps();
		CounterMovement(x, y, mag);
		if (readyToJump && jumping)
		{
			Jump();
		}
		float num3 = walkSpeed;
		if (sprinting)
		{
			num3 = runSpeed;
		}
		if (crouching && grounded && readyToJump)
		{
			rb.AddForce(Vector3.down * Time.deltaTime * 3000f);
			return;
		}
		if (x > 0f && num > num3)
		{
			x = 0f;
		}
		if (x < 0f && num < 0f - num3)
		{
			x = 0f;
		}
		if (y > 0f && num2 > num3)
		{
			y = 0f;
		}
		if (y < 0f && num2 < 0f - num3)
		{
			y = 0f;
		}
		float d = 1f;
		float d2 = 1f;
		if (!grounded)
		{
			d = 0.5f;
			d2 = 0.5f;
		}
		if (grounded && crouching)
		{
			d2 = 0f;
		}
		if (wallRunning)
		{
			d2 = 0.3f;
			d = 0.3f;
		}
		if (surfing)
		{
			d = 0.7f;
			d2 = 0.3f;
		}
		rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * d * d2);
		rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * d);
		SpeedLines();
	}

	private void SpeedLines()
	{
		float num = Vector3.Angle(rb.velocity, playerCam.transform.forward) * 0.15f;
		if (num < 1f)
		{
			num = 1f;
		}
		float rateOverTimeMultiplier = rb.velocity.magnitude / num;
		if (grounded && !wallRunning)
		{
			rateOverTimeMultiplier = 0f;
		}
		psEmission.rateOverTimeMultiplier = rateOverTimeMultiplier;
	}

	private void CameraShake()
	{
		float num = rb.velocity.magnitude / 9f;
		CameraShaker.Instance.ShakeOnce(num, 0.1f * num, 0.25f, 0.2f);
		Invoke("CameraShake", 0.2f);
	}

	private void ResetJump()
	{
		readyToJump = true;
	}

	private void Jump()
	{
		if ((grounded || wallRunning || surfing) && readyToJump)
		{
			MonoBehaviour.print("jumping");
			Vector3 velocity = rb.velocity;
			readyToJump = false;
			rb.AddForce(Vector2.up * jumpForce * 1.5f);
			rb.AddForce(normalVector * jumpForce * 0.5f);
			if (rb.velocity.y < 0.5f)
			{
				rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
			}
			else if (rb.velocity.y > 0f)
			{
				rb.velocity = new Vector3(velocity.x, velocity.y / 2f, velocity.z);
			}
			if (wallRunning)
			{
				rb.AddForce(wallNormalVector * jumpForce * 3f);
			}
			Invoke("ResetJump", jumpCooldown);
			if (wallRunning)
			{
				wallRunning = false;
			}
			AudioManager.Instance.PlayJump();
		}
	}

	private void Look()
	{
		float num = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
		float num2 = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
		Vector3 eulerAngles = playerCam.transform.localRotation.eulerAngles;
		desiredX = eulerAngles.y + num;
		xRotation -= num2;
		xRotation = Mathf.Clamp(xRotation, -90f, 90f);
		FindWallRunRotation();
		actualWallRotation = Mathf.SmoothDamp(actualWallRotation, wallRunRotation, ref wallRotationVel, 0.2f);
		playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, actualWallRotation);
		orientation.transform.localRotation = Quaternion.Euler(0f, desiredX, 0f);
	}

	private void CounterMovement(float x, float y, Vector2 mag)
	{
		if (!grounded || jumping || exploded)
		{
			return;
		}
		float d = 0.16f;
		float num = 0.01f;
		if (crouching)
		{
			rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideSlowdown);
			return;
		}
		if ((Math.Abs(mag.x) > num && Math.Abs(x) < 0.05f) || (mag.x < 0f - num && x > 0f) || (mag.x > num && x < 0f))
		{
			rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * (0f - mag.x) * d);
		}
		if ((Math.Abs(mag.y) > num && Math.Abs(y) < 0.05f) || (mag.y < 0f - num && y > 0f) || (mag.y > num && y < 0f))
		{
			rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * (0f - mag.y) * d);
		}
		if (Mathf.Sqrt(Mathf.Pow(rb.velocity.x, 2f) + Mathf.Pow(rb.velocity.z, 2f)) > walkSpeed)
		{
			float num2 = rb.velocity.y;
			Vector3 vector = rb.velocity.normalized * walkSpeed;
			rb.velocity = new Vector3(vector.x, num2, vector.z);
		}
	}

	public void Explode()
	{
		exploded = true;
		Invoke("StopExplosion", 0.1f);
	}

	private void StopExplosion()
	{
		exploded = false;
	}

	public Vector2 FindVelRelativeToLook()
	{
		float current = orientation.transform.eulerAngles.y;
		float target = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * 57.29578f;
		float num = Mathf.DeltaAngle(current, target);
		float num2 = 90f - num;
		float magnitude = rb.velocity.magnitude;
		return new Vector2(y: magnitude * Mathf.Cos(num * ((float)Math.PI / 180f)), x: magnitude * Mathf.Cos(num2 * ((float)Math.PI / 180f)));
	}

	private void FindWallRunRotation()
	{
		if (!wallRunning)
		{
			wallRunRotation = 0f;
			return;
		}
		_ = new Vector3(0f, playerCam.transform.rotation.y, 0f).normalized;
		new Vector3(0f, 0f, 1f);
		float num = 0f;
		float current = playerCam.transform.rotation.eulerAngles.y;
		if (Math.Abs(wallNormalVector.x - 1f) < 0.1f)
		{
			num = 90f;
		}
		else if (Math.Abs(wallNormalVector.x - -1f) < 0.1f)
		{
			num = 270f;
		}
		else if (Math.Abs(wallNormalVector.z - 1f) < 0.1f)
		{
			num = 0f;
		}
		else if (Math.Abs(wallNormalVector.z - -1f) < 0.1f)
		{
			num = 180f;
		}
		num = Vector3.SignedAngle(new Vector3(0f, 0f, 1f), wallNormalVector, Vector3.up);
		float num2 = Mathf.DeltaAngle(current, num);
		wallRunRotation = (0f - num2 / 90f) * 15f;
		if (!readyToWallrun)
		{
			return;
		}
		if ((Mathf.Abs(wallRunRotation) < 4f && y > 0f && Math.Abs(x) < 0.1f) || (Mathf.Abs(wallRunRotation) > 22f && y < 0f && Math.Abs(x) < 0.1f))
		{
			if (!cancelling)
			{
				cancelling = true;
				CancelInvoke("CancelWallrun");
				Invoke("CancelWallrun", 0.2f);
			}
		}
		else
		{
			cancelling = false;
			CancelInvoke("CancelWallrun");
		}
	}

	private void CancelWallrun()
	{
		MonoBehaviour.print("cancelled");
		Invoke("GetReadyToWallrun", 0.1f);
		rb.AddForce(wallNormalVector * 600f);
		readyToWallrun = false;
		AudioManager.Instance.PlayLanding();
	}

	private void GetReadyToWallrun()
	{
		readyToWallrun = true;
	}

	private void WallRunning()
	{
		if (wallRunning)
		{
			rb.AddForce(-wallNormalVector * Time.deltaTime * moveSpeed);
			rb.AddForce(Vector3.up * Time.deltaTime * rb.mass * 100f * wallRunGravity);
		}
	}

	private bool IsFloor(Vector3 v)
	{
		return Vector3.Angle(Vector3.up, v) < maxSlopeAngle;
	}

	private bool IsSurf(Vector3 v)
	{
		float num = Vector3.Angle(Vector3.up, v);
		if (num < 89f)
		{
			return num > maxSlopeAngle;
		}
		return false;
	}

	private bool IsWall(Vector3 v)
	{
		return Math.Abs(90f - Vector3.Angle(Vector3.up, v)) < 0.1f;
	}

	private bool IsRoof(Vector3 v)
	{
		return v.y == -1f;
	}

	private void StartWallRun(Vector3 normal)
	{
		if (!grounded && readyToWallrun)
		{
			wallNormalVector = normal;
			float d = 20f;
			if (!wallRunning)
			{
				rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
				rb.AddForce(Vector3.up * d, ForceMode.Impulse);
			}
			wallRunning = true;
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
		{
			KillEnemy(other);
		}
	}

	private void OnCollisionExit(Collision other)
	{
	}

	private void OnCollisionStay(Collision other)
	{
		int layer = other.gameObject.layer;
		if ((int)whatIsGround != ((int)whatIsGround | (1 << layer)))
		{
			return;
		}
		for (int i = 0; i < other.contactCount; i++)
		{
			Vector3 normal = other.contacts[i].normal;
			if (IsFloor(normal))
			{
				if (wallRunning)
				{
					wallRunning = false;
				}
				if (!grounded && crouching)
				{
					AudioManager.Instance.Play("StartSlide");
					AudioManager.Instance.Play("Slide");
				}
				grounded = true;
				normalVector = normal;
				cancellingGrounded = false;
				CancelInvoke("StopGrounded");
			}
			if (IsWall(normal) && layer == LayerMask.NameToLayer("Ground"))
			{
				if (!onWall)
				{
					AudioManager.Instance.Play("StartSlide");
					AudioManager.Instance.Play("Slide");
				}
				StartWallRun(normal);
				onWall = true;
				cancellingWall = false;
				CancelInvoke("StopWall");
			}
			if (IsSurf(normal))
			{
				surfing = true;
				cancellingSurf = false;
				CancelInvoke("StopSurf");
			}
			IsRoof(normal);
		}
		float num = 3f;
		if (!cancellingGrounded)
		{
			cancellingGrounded = true;
			Invoke("StopGrounded", Time.deltaTime * num);
		}
		if (!cancellingWall)
		{
			cancellingWall = true;
			Invoke("StopWall", Time.deltaTime * num);
		}
		if (!cancellingSurf)
		{
			cancellingSurf = true;
			Invoke("StopSurf", Time.deltaTime * num);
		}
	}

	private void StopGrounded()
	{
		grounded = false;
	}

	private void StopWall()
	{
		onWall = false;
		wallRunning = false;
	}

	private void StopSurf()
	{
		surfing = false;
	}

	private void KillEnemy(Collision other)
	{
		if ((grounded && !crouching) || rb.velocity.magnitude < 3f)
		{
			return;
		}
		Enemy enemy = (Enemy)other.transform.root.GetComponent(typeof(Enemy));
		if ((bool)enemy && !enemy.IsDead())
		{
			UnityEngine.Object.Instantiate(PrefabManager.Instance.enemyHitAudio, other.contacts[0].point, Quaternion.identity);
			RagdollController ragdollController = (RagdollController)other.transform.root.GetComponent(typeof(RagdollController));
			if (grounded && crouching)
			{
				ragdollController.MakeRagdoll(rb.velocity * 1.2f * 34f);
			}
			else
			{
				ragdollController.MakeRagdoll(rb.velocity.normalized * 250f);
			}
			rb.AddForce(rb.velocity.normalized * 2f, ForceMode.Impulse);
			enemy.DropGun(rb.velocity.normalized * 2f);
		}
	}

	public Vector3 GetVelocity()
	{
		return rb.velocity;
	}

	public float GetFallSpeed()
	{
		return rb.velocity.y;
	}

	public Vector3 GetGrapplePoint()
	{
		return detectWeapons.GetGrapplerPoint();
	}

	public Collider GetPlayerCollider()
	{
		return playerCollider;
	}

	public Transform GetPlayerCamTransform()
	{
		return playerCam.transform;
	}

	public Vector3 HitPoint()
	{
		RaycastHit[] array = Physics.RaycastAll(playerCam.transform.position, playerCam.transform.forward, (int)whatIsHittable);
		if (array.Length < 1)
		{
			return playerCam.transform.position + playerCam.transform.forward * 100f;
		}
		if (array.Length > 1)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].transform.gameObject.layer == LayerMask.NameToLayer("Enemy"))
				{
					return array[i].point;
				}
			}
		}
		return array[0].point;
	}

	public float GetRecoil()
	{
		return detectWeapons.GetRecoil();
	}

	public void KillPlayer()
	{
		if (!Game.Instance.done)
		{
			CameraShaker.Instance.ShakeOnce(3f * GameState.Instance.cameraShake, 2f, 0.1f, 0.6f);
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			UIManger.Instance.DeadUI(b: true);
			Timer.Instance.Stop();
			dead = true;
			rb.freezeRotation = false;
			playerCollider.material = deadMat;
			detectWeapons.Throw(Vector3.zero);
			paused = false;
			ResetSlowmo();
		}
	}

	public void Respawn()
	{
		detectWeapons.StopUse();
	}

	public void Slowmo(float timescale, float length)
	{
		if (GameState.Instance.slowmo)
		{
			CancelInvoke("Slowmo");
			desiredTimeScale = timescale;
			Invoke("ResetSlowmo", length);
			AudioManager.Instance.Play("SlowmoStart");
		}
	}

	private void ResetSlowmo()
	{
		desiredTimeScale = 1f;
		AudioManager.Instance.Play("SlowmoEnd");
	}

	public bool IsCrouching()
	{
		return crouching;
	}

	public bool HasGun()
	{
		return detectWeapons.HasGun();
	}

	public bool IsDead()
	{
		return dead;
	}

	public Rigidbody GetRb()
	{
		return rb;
	}

	private void UpdateActionMeter()
	{
		float target = 0.09f;
		if (rb.velocity.magnitude > 15f && (!dead || !Game.Instance.playing))
		{
			target = 1f;
		}
		actionMeter = Mathf.SmoothDamp(actionMeter, target, ref vel, 0.7f);
	}

	public float GetActionMeter()
	{
		return actionMeter * 22000f;
	}
}

// PlayerAudio
using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
	private Rigidbody rb;

	public AudioSource wind;

	public AudioSource foley;

	private float currentVol;

	private float volVel;

	private void Start()
	{
		rb = PlayerMovement.Instance.GetRb();
	}

	private void Update()
	{
		if (!rb)
		{
			return;
		}
		float num = rb.velocity.magnitude;
		if (PlayerMovement.Instance.grounded)
		{
			if (num < 20f)
			{
				num = 0f;
			}
			num = (num - 20f) / 30f;
		}
		else
		{
			num = (num - 10f) / 30f;
		}
		if (num > 1f)
		{
			num = 1f;
		}
		num *= 1f;
		currentVol = Mathf.SmoothDamp(currentVol, num, ref volVel, 0.2f);
		if (PlayerMovement.Instance.paused)
		{
			currentVol = 0f;
		}
		foley.volume = currentVol;
		wind.volume = currentVol * 0.5f;
	}
}

// PlayerInput
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
	}
}
