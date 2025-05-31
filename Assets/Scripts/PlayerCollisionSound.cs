using UnityEngine;
using System.Collections;

public class PlayerCollisionSound : MonoBehaviour
{
    [Tooltip("拖拽碰撞时播放的声音片段到这里")]
    public AudioClip collisionSoundClip;

    [Tooltip("播放声音后的冷却时间（秒）")]
    public float soundCooldown = 0.8f; // 你可以调整这个值

    private AudioSource audioSource;
    private bool canPlaySoundAfterCooldown = true; // 标记冷却是否结束
    private GameObject lastHitObstacle = null;     // 记录上一个发出声音的障碍物

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("PlayerCollisionSound: AudioSource component missing!");
            enabled = false;
            return;
        }
        if (collisionSoundClip == null)
        {
            Debug.LogWarning("PlayerCollisionSound: CollisionSoundClip not assigned!");
        }
        canPlaySoundAfterCooldown = true;
        lastHitObstacle = null; // 确保游戏开始时为 null
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 调试信息：了解所有碰撞
        Debug.Log("OnControllerColliderHit - Collided with: " + hit.gameObject.name + ", Tag: '" + hit.gameObject.tag + "'");

        // 只对标记为 "Obstacle" 的物体进行处理
        if (hit.gameObject.CompareTag("Obstacle"))
        {
            // 播放声音的条件：
            // 1. 冷却已结束 (canPlaySoundAfterCooldown is true)
            // 2. 并且 (当前撞到的障碍物与上一个发出声音的障碍物不同，或者上一个记录为空)
            if (canPlaySoundAfterCooldown && (lastHitObstacle == null || lastHitObstacle != hit.gameObject))
            {
                Debug.Log("Hit a NEW or DIFFERENT 'Obstacle' after cooldown: " + hit.gameObject.name + ". Attempting to play sound.");

                if (audioSource != null && collisionSoundClip != null)
                {
                    audioSource.PlayOneShot(collisionSoundClip);
                    lastHitObstacle = hit.gameObject;         // 记录当前发出声音的障碍物
                    canPlaySoundAfterCooldown = false;        // 进入冷却状态
                    StopAllCoroutines();                      // 停止任何可能正在运行的旧冷却协程
                    StartCoroutine(SoundCooldownRoutine());   // 启动新的冷却协程
                }
                else
                {
                    if (collisionSoundClip == null)
                        Debug.LogWarning("PlayerCollisionSound: CollisionSoundClip is null. Cannot play sound for Obstacle.");
                }
            }
            else if (!canPlaySoundAfterCooldown) // 如果在冷却中
            {
                Debug.Log("Sound is on cooldown. Hit obstacle: " + hit.gameObject.name);
            }
            else if (lastHitObstacle == hit.gameObject && canPlaySoundAfterCooldown) // 冷却结束了，但还是同一个物体
            {
                Debug.Log("Still in contact with the same obstacle '" + hit.gameObject.name + "' after cooldown. Sound not replayed (waiting for a new obstacle or re-collision after separation).");
            }
        }
        else // 如果撞到的不是 "Obstacle" (例如地面或其他物体)
        {
            // 如果之前记录了一个碰撞的障碍物，而现在撞到了非障碍物，
            // 这意味着玩家已经脱离了上一个障碍物。此时重置 lastHitObstacle。
            if (lastHitObstacle != null)
            {
                Debug.Log("Moved away from last hit obstacle '" + lastHitObstacle.name + "' by hitting a non-obstacle ('" + hit.gameObject.name + "'). Resetting lastHitObstacle.");
                lastHitObstacle = null;
                // 注意：这里不直接重置 canPlaySoundAfterCooldown。
                // 冷却仍然由其自身的协程控制。
                // 重置 lastHitObstacle 意味着下一次再撞到任何障碍物（包括刚脱离的那个），
                // 只要冷却结束，就可以发声。
            }
        }
    }

    IEnumerator SoundCooldownRoutine()
    {
        Debug.Log("Sound cooldown started for " + soundCooldown + " seconds. Last hit obstacle during this cooldown was: " + (lastHitObstacle ? lastHitObstacle.name : "None"));
        yield return new WaitForSeconds(soundCooldown);

        canPlaySoundAfterCooldown = true;
        // lastHitObstacle 不在这里重置
        Debug.Log("Sound cooldown finished. Can play sound again IF the next hit is a new obstacle or if lastHitObstacle was reset due to hitting a non-obstacle.");
    }
}