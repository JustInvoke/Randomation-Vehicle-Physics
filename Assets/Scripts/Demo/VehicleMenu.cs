using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Demo Scripts/Vehicle Menu", 0)]

    //Class for the menu and HUD in the demo
    public class VehicleMenu : MonoBehaviour
    {
        public CameraControl cam;
        public Vector3 spawnPoint;
        public Vector3 spawnRot;
        public GameObject[] vehicles;
        public GameObject chaseVehicle;
        public GameObject chaseVehicleDamage;
        float chaseCarSpawnTime;
        GameObject newVehicle;
        bool autoShift;
        bool assist;
        bool stuntMode;
        public Toggle autoShiftToggle;
        public Toggle assistToggle;
        public Toggle stuntToggle;
        public Text speedText;
        public Text gearText;
        public Slider rpmMeter;
        public Slider boostMeter;
        public Text propertySetterText;
        public Text stuntText;
        public Text scoreText;
        public Toggle camToggle;
        VehicleParent vp;
        Motor engine;
        Transmission trans;
        GearboxTransmission gearbox;
        ContinuousTransmission varTrans;
        StuntDetect stunter;
        float stuntEndTime = -1;
        PropertyToggleSetter propertySetter;

        void Update()
        {
            autoShift = autoShiftToggle.isOn;
            assist = assistToggle.isOn;
            stuntMode = stuntToggle.isOn;
            cam.stayFlat = camToggle.isOn;
            chaseCarSpawnTime = Mathf.Max(0, chaseCarSpawnTime - Time.deltaTime);

            if (vp)
            {
                speedText.text = (vp.velMag * 2.23694f).ToString("0") + " MPH";

                if (trans)
                {
                    if (gearbox)
                    {
                        gearText.text = "Gear: " + (gearbox.currentGear == 0 ? "R" : (gearbox.currentGear == 1 ? "N" : (gearbox.currentGear - 1).ToString()));
                    }
                    else if (varTrans)
                    {
                        gearText.text = "Ratio: " + varTrans.currentRatio.ToString("0.00");
                    }
                }

                if (engine)
                {
                    rpmMeter.value = engine.targetPitch;

                    if (engine.maxBoost > 0)
                    {
                        boostMeter.value = engine.boost / engine.maxBoost;
                    }
                }

                if (stuntMode && stunter)
                {
                    stuntEndTime = string.IsNullOrEmpty(stunter.stuntString) ? Mathf.Max(0, stuntEndTime - Time.deltaTime) : 2;

                    if (stuntEndTime == 0)
                    {
                        stuntText.text = "";
                    }
                    else if (!string.IsNullOrEmpty(stunter.stuntString))
                    {
                        stuntText.text = stunter.stuntString;
                    }

                    scoreText.text = "Score: " + stunter.score.ToString("n0");
                }

                if (propertySetter)
                {
                    propertySetterText.text = propertySetter.currentPreset == 0 ? "Normal Steering" : (propertySetter.currentPreset == 1 ? "Skid Steering" : "Crab Steering");
                }
            }
        }

        public void SpawnVehicle(int vehicle)
        {
            newVehicle = Instantiate(vehicles[vehicle], spawnPoint, Quaternion.LookRotation(spawnRot, GlobalControl.worldUpDir)) as GameObject;
            cam.target = newVehicle.transform;
            cam.Initialize();
            vp = newVehicle.GetComponent<VehicleParent>();

            trans = newVehicle.GetComponentInChildren<Transmission>();
            if (trans)
            {
                trans.automatic = autoShift;
                newVehicle.GetComponent<VehicleParent>().brakeIsReverse = autoShift;

                if (trans is GearboxTransmission)
                {
                    gearbox = trans as GearboxTransmission;
                }
                else if (trans is ContinuousTransmission)
                {
                    varTrans = trans as ContinuousTransmission;

                    if (!autoShift)
                    {
                        vp.brakeIsReverse = true;
                    }
                }
            }

            if (newVehicle.GetComponent<VehicleAssist>())
            {
                newVehicle.GetComponent<VehicleAssist>().enabled = assist;
            }

            if (newVehicle.GetComponent<FlipControl>() && newVehicle.GetComponent<StuntDetect>())
            {
                newVehicle.GetComponent<FlipControl>().flipPower = stuntMode && assist ? new Vector3(10, 10, -10) : Vector3.zero;
                newVehicle.GetComponent<FlipControl>().rotationCorrection = stuntMode ? Vector3.zero : (assist ? new Vector3(5, 1, 10) : Vector3.zero);
                newVehicle.GetComponent<FlipControl>().stopFlip = assist;
                stunter = newVehicle.GetComponent<StuntDetect>();
            }

            engine = newVehicle.GetComponentInChildren<Motor>();
            propertySetter = newVehicle.GetComponent<PropertyToggleSetter>();

            stuntText.gameObject.SetActive(stuntMode);
            scoreText.gameObject.SetActive(stuntMode);
        }

        public void SpawnChaseVehicle()
        {
            if (chaseCarSpawnTime == 0)
            {
                chaseCarSpawnTime = 1;
                GameObject chaseCar = Instantiate(chaseVehicle, spawnPoint, Quaternion.LookRotation(spawnRot, GlobalControl.worldUpDir)) as GameObject;
                chaseCar.GetComponent<FollowAI>().target = newVehicle.transform;
            }
        }

        public void SpawnChaseVehicleDamage()
        {
            if (chaseCarSpawnTime == 0)
            {
                chaseCarSpawnTime = 1;
                GameObject chaseCar = Instantiate(chaseVehicleDamage, spawnPoint, Quaternion.LookRotation(spawnRot, GlobalControl.worldUpDir)) as GameObject;
                chaseCar.GetComponent<FollowAI>().target = newVehicle.transform;
            }
        }
    }
}