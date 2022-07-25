using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    // Values that are only edited in editor.
    public float timeBetweenActions = 0.5f;
    public float speed = 5f;
    private float swingTime = 0.35f;

    public GameController gameController;
    public BoxCollider2D leftCollider;
    public BoxCollider2D rightCollider;
    public BoxCollider2D topCollider;
    public BoxCollider2D bottomCollider;
    public TilemapCollider2D wallsCollider;
    public SpriteRenderer sr;
    
    public AudioSource doorOpen;
    public AudioSource keyClink;
    public AudioSource footsteps;
    public AudioSource swordPickup;
    public AudioSource swordMiss;
    public AudioSource swordHit;

    public Sprite spriteWithSword;
    public GameObject swingAnimPrefab;

    private bool isMoving = false;
    private Vector2 moveTargetLocation;

    private ItemScriptableObject[] itemsInInventory = new ItemScriptableObject[3];

    private bool hasSword = false;

    private int treasuresCollected;
    private int monstersKilled;

    public int GetTreasuresCollected() {
        return treasuresCollected;
    }

    public int GetMonstersKilled() {
        return monstersKilled;
    }

    void Update() {
        // If we are supposed to be moving, move player towards target location.
        // Step is the max distance it can move per frame, controlled by the "speed" variable.
        if (isMoving) {
            float step = speed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, moveTargetLocation, step);
        }
    }

    /// <summary>
    /// Executes the given actions.
    /// </summary>
    /// <param name="actions">A list of strings that correspond to different actions in Execute().</param>
    public IEnumerator ExecuteActions(List<string> actions) {
        foreach (string a in actions) {
            Execute(a);
            yield return new WaitForSeconds(timeBetweenActions);
            isMoving = false;
        }
    }

    /// <summary>
    /// Performs the given action (string). Logs errors, but this should never happen at runtime.
    /// </summary>
    /// <param name="action">String that corresponds to an action.</param>
    public void Execute(string action) {
        switch(action) {
            case "moveleft":
                Move(-1,0,leftCollider);
                break;
            case "moveright":
                Move(1,0,rightCollider);
                break;
            case "moveup":
                Move(0,1,topCollider);
                break;
            case "movedown":
                Move(0,-1,bottomCollider);
                break;
            case "pickup":
                PickupItem();
                break;
            case "useitem0":
                UseItem(0);
                break;
            case "useitem1":
                UseItem(1);
                break;
            case "useitem2":
                UseItem(2);
                break;
            case "wait":
                // Do nothing; wait for next cycle.
                break;
            case "swingleft":
                Swing(-0.4f, 0, 180, leftCollider);
                break;
            case "swingright":
                Swing(0.4f, 0, 0, rightCollider);
                break;
            case "swingup":
                Swing(0, 0.5f, 90, topCollider);
                break;
            case "swingdown":
                Swing(0, -0.3f, -90, bottomCollider);
                break;
            default:
                // This should never happen in-game!
                Debug.Log("Such action does not exist!");
                break;
        }
    }

    /// <summary>
    /// Makes the player move smoothly to the given position.
    /// </summary>
    /// <param name="x">Player position change on the x-axis.</param>
    /// <param name="y">Player position change on the y-axis.</param>
    private void Move(int x, int y, BoxCollider2D col) {

        if (Physics2D.IsTouching(col, wallsCollider)) {
            return;
        }
        // Change the location that the player is supposed to move to by taking player's current
        // position and adding x and y to it. (For example, when moving left, add -1 and 0.)
        moveTargetLocation = new Vector2(this.transform.position.x + x, this.transform.position.y + y);

        // Begin movement by enabling movement flag.
        isMoving = true;
        footsteps.pitch = Random.Range(0.75f,1.25f);
        footsteps.Play();
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.tag == "Monster") {
            gameController.queueShouldBeStopped = true;

            // Temporary since there is no death animation implemented yet.
            sr.sprite = null;
        }

        if (other.tag == "Door") {
            DoorController doorController = other.gameObject.GetComponent<DoorController>();
            if (doorController.amountOfKeysNeeded == 0) {
                StartCoroutine(WinLevel());
            }
        }
    }

    private void PickupItem() {
        Collider2D[] collisions = Physics2D.OverlapCircleAll(this.transform.position, 0.1f);
        foreach (Collider2D other in collisions) {
            if (other.tag == "Item") {
                ItemScriptableObject item = other.gameObject.GetComponent<ItemController>().item;

                if (item.itemName == "sword") {
                    gameController.ToggleSwordVisibility(true);
                    hasSword = true;
                    swordPickup.Play();
                    Destroy(other.gameObject);
                    sr.sprite = spriteWithSword;
                    return;
                }

                for (int i = 0; i < itemsInInventory.Length; i++) {
                    if (itemsInInventory[i] == null) {
                        itemsInInventory[i] = item;
                        gameController.inventoryRenderers[i].sprite = item.sprite;
                        Destroy(other.gameObject);
                        keyClink.Play();

                        if (item.itemName == "gem") {
                            treasuresCollected++;
                        }
                        break;
                    }
                } 
            }
        }
    }
    
    private void UseItem(int index) {
        if (itemsInInventory[index] == null) {
            return;
        }

        Collider2D[] collisions = Physics2D.OverlapCircleAll(this.transform.position, 0.1f);
        foreach (Collider2D other in collisions) {
            if (other.tag == "Door" && itemsInInventory[index].itemName == "key") {
                DoorController doorController = other.gameObject.GetComponent<DoorController>();
                gameController.inventoryRenderers[index].sprite = null;
                itemsInInventory[index] = null;
                if (doorController.UseKey() == 0) {
                    StartCoroutine(WinLevel());
                }
            }
        }
    }

    private void Swing(float x, float y, int angle, BoxCollider2D col) {
        if (!hasSword) {
            return;
        }

        Vector2 posOfSwing = this.transform.position;
        posOfSwing.x += x;
        posOfSwing.y += y;
        GameObject go = Instantiate(swingAnimPrefab, posOfSwing, Quaternion.Euler(new Vector3(0, 0, angle)));
        Destroy(go, swingTime);

        Collider2D[] collisions = Physics2D.OverlapCircleAll(col.gameObject.transform.position, 0.1f);
        foreach (Collider2D other in collisions) {
            if (other.tag == "Monster") {
                gameController.DestroyMonster(other.gameObject);
                monstersKilled++;
                swordHit.Play();
            }
            else {
                swordMiss.Play();
            }
        }
    }

    private IEnumerator WinLevel() {
        doorOpen.Play();
        yield return new WaitForSeconds(1.25f);
        gameController.WinLevel();
    }
}
