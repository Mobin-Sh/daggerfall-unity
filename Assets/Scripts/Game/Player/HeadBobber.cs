using UnityEngine;
using System;
using System.Collections;
using DaggerfallConnect;

namespace DaggerfallWorkshop.Game
{
    public enum BobbingStyle
    {
        Crouching,
        Walking,
        Running,
        Horse
    }

    [RequireComponent(typeof(CharacterController))]
    public class HeadBobber : MonoBehaviour
    {
        private BobbingStyle bobStyle = BobbingStyle.Walking;
        public BobbingStyle BobStyle
        {
            get { return bobStyle; }
        }
        private PlayerMotor playerMotor;
        private Camera mainCamera;

        public Vector3 restPos; //local position where your camera would rest when it's not bobbing.
        
        public float transitionSpeed = 20f; //smooths out the transition from moving to not moving.
        public float bobSpeed; //how quickly the player's head bobs.
        public float bobXAmount; //how dramatic the bob is in side motion.
        public float bobYAmount; //how dramatic the bob is in up/down motion.
        public float bobScalar; // user controlled multiplier for strength of bob

        float timer = Mathf.PI / 2; //initialized as this value because this is where sin = 1. So, this will make the camera always start at the crest of the sin wave, simulating someone picking up their foot and starting to walk--you experience a bob upwards when you start walking as your foot pushes off the ground, the left and right bobs come as you walk.
        float beginTransitionTimer = 0; // timer for smoothing out beginning of headbob.
        float endTransitionTimer = 0; // timer for smoothing out end of headbob. 
        const float endTimerMax = 0.5f;
        const float beginTimerMax = Mathf.PI;
        private bool bIsStopping;

        void Start()
        {
            playerMotor = GetComponent<PlayerMotor>();
            
            mainCamera = GameManager.Instance.MainCamera;
            restPos = mainCamera.transform.localPosition;
            
            bobScalar = 1.0f;
            bobSpeed = 1.20f;
            bIsStopping = false;
        }

        void Update()
        {
            if (DaggerfallUnity.Settings.HeadBobbing == false ||
                GameManager.Instance.PlayerEntity.CurrentHealth < 1 ||
                GameManager.IsGamePaused)
                return;

            GetBobbingStyle();
            SetParamsForBobbingStyle();

            Vector3 newCameraPosition = getNewPos();
            mainCamera.transform.localPosition = newCameraPosition;
        }

        public virtual void GetBobbingStyle()
        {
            if (playerMotor.IsRunning)
                bobStyle = BobbingStyle.Running;
            else if (playerMotor.IsCrouching)
                bobStyle = BobbingStyle.Crouching;
            else if (playerMotor.IsRiding)
                bobStyle = BobbingStyle.Horse;
            else
                bobStyle = BobbingStyle.Walking;

        }

        public virtual void SetParamsForBobbingStyle()
        {
            
            switch (bobStyle)
            {
                // TODO: adjust bob speed to match player footstep sound better
                case BobbingStyle.Crouching:
                    // lot of swaying side to side as shifting legs and pushing up and off each leg
                    bobXAmount = 0.08f * bobScalar;
                    bobYAmount = 0.07f * bobScalar;
                    break;
                case BobbingStyle.Walking:
                    // More y than x because walking is pretty balanced side to side, just head bounce
                    bobXAmount = 0.045f * bobScalar;
                    bobYAmount = 0.062f * bobScalar;
                    break;
                case BobbingStyle.Running:
                    // both legs pushing off ground and lots of leaning side to side.
                    bobXAmount = 0.09f * bobScalar;
                    bobYAmount = 0.11f * bobScalar;
                    break;
                case BobbingStyle.Horse:
                    // horse has 4 legs: balanced, most force pushes player up.
                    bobXAmount = 0.03f * bobScalar;
                    bobYAmount = 0.115f * bobScalar;
                    break;
                default:
                    // error
                    break;
            }
        }

        public virtual Vector3 getNewPos()
        { 
            Vector3 newPosition = restPos;
            float velocity = new Vector2(playerMotor.MoveDirection.x, playerMotor.MoveDirection.z).magnitude;
            
            if ((Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0) && playerMotor.IsGrounded)
            {   // player is moving on ground
                
                timer += velocity * bobSpeed * Time.deltaTime;
                beginTransitionTimer += velocity * bobSpeed * Time.deltaTime;

                newPosition = PlotPath();

                if (beginTransitionTimer <= Mathf.PI)
                {
                    newPosition = InterpolateBeginTransition(newPosition); // smooth out start of player's movement
                }
                bIsStopping = true; // next branch of if/else will evaluate to true only after releasing keys.
                endTransitionTimer = 0;
            }
            else if (bIsStopping && endTransitionTimer <= endTimerMax)
            {
                // player is stopping moving now
                //Debug.Log("Stopped moving");
                timer = Mathf.PI; //reinitialize for next start
                beginTransitionTimer = 0; // reset

                endTransitionTimer += Time.deltaTime;

                newPosition = InterpolateEndTransition(endTransitionTimer);
            }
            else if (bIsStopping)// endTransitionTimer reached max
            {
                Debug.Log("End Transition Reset");
                endTransitionTimer = 0;
                bIsStopping = false;
            }

            if (timer > Mathf.PI * 2 ) //completed a full cycle on the unit circle. Reset to 0 to avoid bloated values.
            {
                timer = 0;
            }

            return newPosition;
        }

        public virtual Vector3 PlotPath()
        {
            return new Vector3(Mathf.Cos(timer) * bobXAmount, restPos.y + Mathf.Abs((Mathf.Sin(timer) * bobYAmount)), restPos.z); //abs val of y for a parabolic path
        }

        public Vector3 InterpolateEndTransition(float endTimer) // interpolates a gradual path from moving to not moving.
        {
            float t = (endTimer / endTimerMax); 
            Vector3 camPos = mainCamera.transform.localPosition;
            return Vector3.Lerp(camPos, restPos, t);
        }

        public Vector3 InterpolateBeginTransition(Vector3 newPosition) // interpolates a gradual path from not moving to moving.
        {
            float t = (timer % Mathf.PI) / Mathf.PI;
            return Vector3.Lerp(restPos, newPosition, t);
        }


    }
}


