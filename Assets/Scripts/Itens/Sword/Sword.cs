using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Executa o golpe da espada, ativa a área de dano durante a animação e acompanha a mira do mouse.
/// </summary>
public class Sword : MonoBehaviour
{
    [SerializeField] private GameObject slashAnimPrefab;
    [SerializeField] private Transform slashAnimSpawnPoint;
    [SerializeField] private Transform weaponCollider;
    [SerializeField] private float swordAttackCD = .32f;

    private Animator myAnimator;
    private PlayerController playerController;
    private ActiveWeapon activeWeapon;
    private Collider2D attackCollider;
    private DamageSource damageSource;
    private bool isAttacking;

    private GameObject slashAnim;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        MouseFollowWithOffset();
    }

    private void OnDisable()
    {
        SetAttackColliderActive(false);
    }

    public void SetDamage(int damage)
    {
        CacheReferences();
        if (damageSource != null)
        {
            damageSource.SetDamageAmount(damage);
        }
    }

    // Inicia a animação, habilita temporariamente o collider de dano e aplica a recarga da espada.
    public void PerformAttack(int damage)
    {
        CacheReferences();
        if (!isActiveAndEnabled || isAttacking || weaponCollider == null ||
            slashAnimPrefab == null || slashAnimSpawnPoint == null)
        {
            return;
        }

        isAttacking = true;
        SetDamage(damage);
        if (myAnimator != null)
        {
            myAnimator.SetTrigger("Attack");
        }

        SetAttackColliderActive(true);
        slashAnim = Instantiate(slashAnimPrefab, slashAnimSpawnPoint.position, Quaternion.identity);
        slashAnim.transform.SetParent(transform.parent, true);
        StartCoroutine(AttackCDRoutine());
    }

    private IEnumerator AttackCDRoutine() {
        yield return new WaitForSeconds(swordAttackCD);
        isAttacking = false;
    }

    // Animation Event que encerra a janela de dano para evitar acertos fora do golpe.
    public void DoneAttackingAnimEvent()
    {
        SetAttackColliderActive(false);
    }


    public void SwingUpFlipAnimEvent()
    {
        if (slashAnim == null)
        {
            return;
        }

        slashAnim.transform.rotation = Quaternion.Euler(-180, 0, 0);

        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (playerController != null && playerController.FacingLeft && slashRenderer != null)
        {
            slashRenderer.flipX = true;
        }
    }

    public void SwingDownFlipAnimEvent()
    {
        if (slashAnim == null)
        {
            return;
        }

        slashAnim.transform.rotation = Quaternion.Euler(0, 0, 0);

        SpriteRenderer slashRenderer = slashAnim.GetComponent<SpriteRenderer>();
        if (playerController != null && playerController.FacingLeft && slashRenderer != null)
        {
            slashRenderer.flipX = true;
        }
    }

    private void MouseFollowWithOffset()
    {
        if (playerController == null || activeWeapon == null || weaponCollider == null ||
            Camera.main == null || Mouse.current == null)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float distanceFromCamera = Mathf.Abs(Camera.main.transform.position.z - playerController.transform.position.z);
        Vector3 screenPosition = new Vector3(mousePosition.x, mousePosition.y, distanceFromCamera);
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 direction = mouseWorldPosition - playerController.transform.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bool facingLeft = direction.x < 0f;
        Transform weaponRoot = transform.parent != null && transform.parent != activeWeapon.transform
            ? transform.parent
            : activeWeapon.transform;
        weaponRoot.rotation = Quaternion.Euler(0f, facingLeft ? 180f : 0f, angle);
    }

    public void SetAttackColliderActive(bool active)
    {
        CacheReferences();
        if (attackCollider != null)
        {
            attackCollider.enabled = active;
        }
    }

    private void CacheReferences()
    {
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }

        if (activeWeapon == null)
        {
            activeWeapon = GetComponentInParent<ActiveWeapon>();
        }

        if (myAnimator == null)
        {
            myAnimator = GetComponent<Animator>();
        }

        if (weaponCollider == null && activeWeapon != null)
        {
            Transform colliderTransform = null;
            Transform ancestor = transform.parent;

            // Prefer a collider belonging to this weapon prefab. The old scene
            // weapon used a separate WeaponCollider under the ActiveWeapon root.
            while (ancestor != null && ancestor != activeWeapon.transform)
            {
                if (ancestor.GetComponent<DamageSource>() != null &&
                    ancestor.GetComponent<Collider2D>() != null)
                {
                    colliderTransform = ancestor;
                    break;
                }

                ancestor = ancestor.parent;
            }

            if (colliderTransform == null)
            {
                colliderTransform = activeWeapon.transform.Find("WeaponCollider");
            }

            if (colliderTransform == null)
            {
                Collider2D[] colliders = activeWeapon.GetComponentsInChildren<Collider2D>(true);
                foreach (Collider2D candidate in colliders)
                {
                    if (candidate.GetComponent<DamageSource>() != null)
                    {
                        colliderTransform = candidate.transform;
                        break;
                    }
                }
            }

            weaponCollider = colliderTransform;
        }

        if (attackCollider == null && weaponCollider != null)
        {
            attackCollider = weaponCollider.GetComponent<Collider2D>();
        }

        if (damageSource == null && weaponCollider != null)
        {
            damageSource = weaponCollider.GetComponent<DamageSource>();
        }
    }
}
