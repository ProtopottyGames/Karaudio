using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Karaudio : MonoBehaviour
{
    [Space(10)]
    [Header("General ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------->")]
    public bool findReferancesByTag = true;
    private bool scriptIsMissingNecessaryReferences = false;

    [Space(5)]
    [Header("Players ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------->")]
    public GameObject player;
    public float playerHeight;
    public bool playerIsUnderwater;

    private bool submergeSplashIsReady = true;
    private bool resurfaceSplashIsReady = false;
    
    [Header("Checkpoints ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------>")]
    [Space(5)]
    public GameObject atmosphere;
    public GameObject waterSurface;
    public GameObject seaFloor;
    public GameObject submerge;
    public GameObject resurface;

    // Height References
    private Vector3 atmosphereCheckpointLocation;
    private Vector3 waterSurfaceCheckpointLocation;
    private Vector3 seaFloorCheckpointLocation;

    // AudioSource References
    private AudioSource atmosphereAudioSource;
    private AudioSource waterSurfaceAudioSource;
    private AudioSource seaFloorAudioSource;

    //Audio Clips
    private AudioClip submergeSplashClip;
    private AudioClip resurfaceSplashClip;

    // Audio Filters
    private AudioLowPassFilter waterSurfaceBGM_LPF;
    private AudioLowPassFilter SubmergingSplashLPF;

    [Space(5)]
    [Header("Crossfades ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------->")]
    [Range(0, 1)]
    public float surface_Atmosphere;
    [Range(0, 1)]
    public float atmosphere_Surface;
    [Range(0, 1)]
    public float floor_Surface;
    [Range(0, 1)]
    public float surface_Floor;

    [Space(5)]
    [Header("Filters ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------->")]
    public bool EnableUnderwaterLPF = true;
    private float LPFCutoffValue;


    void Awake()
    {
        LocateGameObjectReferences();
    }

    void Start()
    {
        AssignmentsAndComponentsSetup();
    }

    void Update()
    {
        if (scriptIsMissingNecessaryReferences)
        {
            return;
        }

        SoundOperations();
    }


    // For Automatically Locating The Necessary Reference Objects In The Scene Via Tag If The User So Chooses To Do So: 
    void LocateGameObjectReferences()
    {
        if (findReferancesByTag)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            atmosphere = GameObject.FindGameObjectWithTag("atmosphereCheckpointTag");
            waterSurface = GameObject.FindGameObjectWithTag("waterSurfaceCheckpointTag");
            seaFloor = GameObject.FindGameObjectWithTag("SeaFloorCheckpointTag");

            submerge = GameObject.FindGameObjectWithTag("SubmergeSplashCheckpointTag");
            resurface = GameObject.FindGameObjectWithTag("ResurfaceSplashCheckpointTag");

            submergeSplashClip = submerge.GetComponent<AudioSource>().clip;
            resurfaceSplashClip = resurface.GetComponent<AudioSource>().clip;
        }
    }

    void AssignmentsAndComponentsSetup()
    {
        ReferenceCop();

        // Register Checkpoint Locations:
        atmosphereCheckpointLocation = atmosphere.transform.position;
        waterSurfaceCheckpointLocation = waterSurface.transform.position;
        seaFloorCheckpointLocation = seaFloor.transform.position;

        // Retrieve AudioSource Components:
        atmosphereAudioSource = atmosphere.GetComponent<AudioSource>();
        waterSurfaceAudioSource = waterSurface.GetComponent<AudioSource>();
        seaFloorAudioSource = seaFloor.GetComponent<AudioSource>();

        // Get Audio Clips For Splashes:
        submergeSplashClip = submerge.GetComponent<AudioSource>().clip;
        resurfaceSplashClip = resurface.GetComponent<AudioSource>().clip;

        // Set AudioSource RollOff Modes To Linear:
        atmosphereAudioSource.rolloffMode = AudioRolloffMode.Linear;
        waterSurfaceAudioSource.rolloffMode = AudioRolloffMode.Linear;
        seaFloorAudioSource.rolloffMode = AudioRolloffMode.Linear;
        submerge.GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Linear;
        resurface.GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Linear;

        // Generate & Disable Underwater Low Pass Filters For Later Use In Update:
        waterSurfaceBGM_LPF = waterSurface.AddComponent<AudioLowPassFilter>();
        SubmergingSplashLPF = submerge.AddComponent<AudioLowPassFilter>();

        waterSurfaceBGM_LPF.enabled = false;
        SubmergingSplashLPF.enabled = false;
    }

    // Operations For Calculating Distances And Activating Audio/Filters:
    void SoundOperations()
    {
        playerHeight = player.gameObject.transform.position.y;

        float atmosphereToWaterSurfaceVolumeInterp = (playerHeight - atmosphereCheckpointLocation.y) / (waterSurfaceCheckpointLocation.y - atmosphereCheckpointLocation.y);
        float waterSurfaceToAtmosphereVolumeInterp = (playerHeight - atmosphereCheckpointLocation.y) / (waterSurfaceCheckpointLocation.y - atmosphereCheckpointLocation.y);
        float waterSurfaceToSeaFloorVolumeInterp = (playerHeight - seaFloorCheckpointLocation.y) / (waterSurfaceCheckpointLocation.y - seaFloorCheckpointLocation.y);
        float seaFloorToWaterSurfaceVolumeInterp = (playerHeight - seaFloorCheckpointLocation.y) / (waterSurfaceCheckpointLocation.y - seaFloorCheckpointLocation.y);

        if (playerHeight < waterSurfaceCheckpointLocation.y)
        {
            playerIsUnderwater = true;

            atmosphere_Surface = Mathf.Lerp(0.0f, 0.0f, atmosphereToWaterSurfaceVolumeInterp);
            surface_Atmosphere = Mathf.Lerp(0.0f, 0.0f, waterSurfaceToAtmosphereVolumeInterp);
            surface_Floor = Mathf.Lerp(1.0f, 0.0f, waterSurfaceToSeaFloorVolumeInterp);
            floor_Surface = Mathf.Lerp(0.0f, 1.0f, seaFloorToWaterSurfaceVolumeInterp);

            atmosphereAudioSource.volume = atmosphere_Surface;
            waterSurfaceAudioSource.volume = floor_Surface;
            seaFloorAudioSource.volume = surface_Floor;

            LPFCutoffValue = surface_Floor;

            if (EnableUnderwaterLPF)
            {
                SubmergingSplashLPF.enabled = true;
                waterSurfaceBGM_LPF.enabled = true;

                SubmergingSplashLPF.GetComponent<AudioLowPassFilter>().cutoffFrequency = Mathf.Lerp(5007.7f, 0.0f, LPFCutoffValue);
                waterSurface.GetComponent<AudioLowPassFilter>().cutoffFrequency = Mathf.Lerp(5007.7f, 0.0f, LPFCutoffValue);
            }

            else if (!EnableUnderwaterLPF || playerHeight > waterSurfaceCheckpointLocation.y)
            {
                waterSurfaceBGM_LPF.enabled = false;
                SubmergingSplashLPF.enabled = false;
            }

            if (submergeSplashIsReady)
            {
                InSplash();
            }
        }

        else if (playerHeight > waterSurfaceCheckpointLocation.y)
        {
            playerIsUnderwater = false;

            waterSurfaceBGM_LPF.enabled = false;
            SubmergingSplashLPF.enabled = false;

            atmosphere_Surface = Mathf.Lerp(0.0f, 1.0f, atmosphereToWaterSurfaceVolumeInterp);
            surface_Atmosphere = Mathf.Lerp(1.0f, 0.0f, waterSurfaceToAtmosphereVolumeInterp);
            surface_Floor = Mathf.Lerp(0.0f, 0.0f, waterSurfaceToSeaFloorVolumeInterp);
            floor_Surface = Mathf.Lerp(0.0f, 0.0f, seaFloorToWaterSurfaceVolumeInterp);

            atmosphereAudioSource.volume = surface_Atmosphere;
            waterSurfaceAudioSource.volume = atmosphere_Surface;
            seaFloorAudioSource.volume = surface_Floor;

            if (resurfaceSplashIsReady)
            {
                OutSplash();
            }
        }
    }

    // Setup For Submerge/Resurface Splashes To Be Used In Update:
    void InSplash()
    {
        // Play SubmergeSplash Clip:
        submerge.GetComponent<AudioSource>().PlayOneShot(submergeSplashClip);

        // Fire Auxiliary AboveWater Function;
        AuxUserFunction_PlayerIsAboveWater();

        // Set Bools To Allow For Only A Single OneShot Of The ResurfaceSplash Clip In Update:
        resurfaceSplashIsReady = true;
        submergeSplashIsReady = false;
    }

    void OutSplash()
    {
        // Play ResurfaceSplash Clip:
        resurface.GetComponent<AudioSource>().PlayOneShot(resurfaceSplashClip);

        // Fire Auxiliary Underwater Function;
        AuxUserFunction_PlayerIsUnderwater();

        // Set Bools To Allow For Only A Single OneShot Of The SubmergeSplash Clip In Update:
        submergeSplashIsReady = true;
        resurfaceSplashIsReady = false;
    }
    
    // Auxillary Code Can Be Added Here Based On Whether The Player Is Above/Below The Surface Of The Water:
    void AuxUserFunction_PlayerIsUnderwater()
    {
       // Debug.Log("The Player Is Currently Above Water");
    }

    void AuxUserFunction_PlayerIsAboveWater()
    {
       // Debug.Log("The Player Is Currently Underwater");
    }

    // If Any References Are Missing, Report Which Ones And Shut Down The Party:
    void ReferenceCop()
    {
        if (!player)
        {
            Debug.Log("Karaudio Error: The Player Reference Object Is Missing!");
            scriptIsMissingNecessaryReferences = true;
        }
        if (!atmosphere)
        {
            Debug.Log("Karaudio Error: The atmosphere Reference Object Is Missing!");
            scriptIsMissingNecessaryReferences = true;
        }
        if (!waterSurface)
        {
            Debug.Log("Karaudio Error: The waterSurface Reference Object Is Missing!");
            scriptIsMissingNecessaryReferences = true;
        }
        if (!seaFloor)
        {
            Debug.Log("Karaudio Error: The seaFloor Reference Object Is Missing!");
            scriptIsMissingNecessaryReferences = true;
        }
        if (!submerge)
        {
            Debug.Log("Karaudio Error: The submerge Reference Object Is Missing!");
            scriptIsMissingNecessaryReferences = true;
        }
        if (!resurface)
        {
            Debug.Log("Karaudio Error: The resurface Reference Object Is Missing!");
            scriptIsMissingNecessaryReferences = true;
        }
        if (!submergeSplashClip)
        {
            scriptIsMissingNecessaryReferences = true;
        }
        if (!resurfaceSplashClip)
        {
            scriptIsMissingNecessaryReferences = true;
        }

        if (scriptIsMissingNecessaryReferences)
        {
            Debug.Log("Karaudio Error: All Necessary Objects Must Be Assigned Befor Continuing!!");

            AudioSource[] allAudioSources;

            allAudioSources = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];

            foreach (AudioSource audioS in allAudioSources)
            {
                audioS.Stop();
            }

            return;
        }
    }

}
