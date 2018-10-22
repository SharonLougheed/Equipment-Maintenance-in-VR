using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OutlinePart : MonoBehaviour {


    public enum Mode { OutlinePart, InteractablePart };
    [Tooltip("Dictates the properties and behavier of the part")]
    private Mode partMode = Mode.OutlinePart;
    public bool checkRotation = true;
    [Tooltip("Amount of deviation from a perfect rotation match on all axes")]
    public float acceptableDegrees = 10f;
    public bool checkPosition = true;
    [Tooltip("Amount of deviation from a perfect overlap in position")]
    public float acceptableMeters = 0.1f;
    [Tooltip("Material used for showing where replacement part is supposed to go. Default is OrangeOutline")]
    public Material defaultOutlineMaterial;
    [Tooltip("Material used for showing the user's replacement part is acceptable. Default is GreenOutline")]
    public Material acceptablePlacementMaterial;
    [Tooltip("Material used for showing the user's replacement part is not acceptable. Default is RedOutline")]
    public Material unacceptablePlacementMaterial;
    [Tooltip("Number of frames to skip between triggers")]
    public int frameSkip = 30;
    private int frameCount = 0;
    public UnityEvent onAcceptablePlacement;
    public UnityEvent onUnacceptablePlacement;

    private Dictionary<int, Material[]> originalMaterials;
    private Dictionary<string, Collider> originalColliders;
    private List<GameObject> allGameObjects;
    private bool isAcceptableRotation = false;
    private bool isAcceptablePosition = false;
    private Bounds selfGroupBounds;
    private Bounds otherGroupBounds;
    private bool selfBoundsExpired = true;
    private bool otherBoundsExpired = true;

    void Awake()
    {
        if (defaultOutlineMaterial == null)
        {
            defaultOutlineMaterial = Resources.Load("Materials/OutlineMatOrange") as Material;
        }
        if (acceptablePlacementMaterial == null)
        {
            acceptablePlacementMaterial = Resources.Load("Materials/OutlineMatGreen") as Material;
        }
        if (unacceptablePlacementMaterial == null)
        {
            unacceptablePlacementMaterial = Resources.Load("Materials/OutlineMatRed") as Material;
        }
    }


    // Use this for initialization
    void Start()
    {
        allGameObjects = FindAllGameObjectsAtOrBelow(gameObject);
        originalMaterials = SaveOriginalMaterials(allGameObjects);
        originalColliders = SaveOriginalColliders(allGameObjects);


        // Extra init stuff
        switch (partMode)
        {
            case Mode.OutlinePart:
                // Make sure main gameobject has a rigidbody so that a compound rigidbody can be created (any lower down rigidbodies will be destroyed later when applying defaults)
                if (gameObject.GetComponent<Rigidbody>() == null)
                {
                    gameObject.AddComponent<Rigidbody>().useGravity = false;
                }
                // TODO figure out why default material is not being set in ApplyModeDefaults, but this works for now
                ApplyMaterialToList(allGameObjects, defaultOutlineMaterial);
                break;
        }
        ApplyModeDefaults(allGameObjects);
    }


    // Update is called once per frame
    void Update()
    {

    }


    void FixedUpdate()
    {
        frameCount++;
    }


    void OnTriggerStay(Collider other)
    {
        // Debug.Log(name + " OnTriggerStay");
        Collider matchingCollider;

        // TODO implement tag system to check that other is part of a replacement part
        if (frameCount % frameSkip != 0
            || partMode != Mode.OutlinePart
            || !originalColliders.TryGetValue(other.name, out matchingCollider))
        {
            return;
        }
        frameCount = 1;
        // Confirmed that the other part is an intended replacement
        // Condition checking
        if (!checkRotation || IsWithinRangeOfRotation(matchingCollider.transform.rotation, other.transform.rotation, acceptableDegrees))
        {
            isAcceptableRotation = true;
        }
        if ((!checkRotation || isAcceptableRotation) && (!checkPosition || CollidersWithinLimit(matchingCollider, other, acceptableMeters)))
        {
            isAcceptablePosition = true;
        }
        // Debug.Log("IsAcceptableRotation: " + isAcceptableRotation + " IsAcceptablePosition: " + isAcceptablePosition);
        if ((!checkRotation || isAcceptableRotation) && (!checkPosition || isAcceptablePosition))
        {
            OnAcceptablePlacement();
            ApplyMaterialToList(allGameObjects, acceptablePlacementMaterial);
        }
        else
        {
            OnUnacceptablePlacement();
            ApplyMaterialToList(allGameObjects, unacceptablePlacementMaterial);
        }
        isAcceptableRotation = false;
        isAcceptablePosition = false;
    }


    void OnTriggerExit(Collider other)
    {
        isAcceptableRotation = false;
        isAcceptablePosition = false;
        otherBoundsExpired = true;
        if (partMode == Mode.OutlinePart)
            ApplyMaterialToList(allGameObjects, defaultOutlineMaterial);
    }


    void ApplyModeDefaultsToSelf()
    {
        ApplyModeDefaults(gameObject);
    }


    void ApplyModeDefaultsToChildren()
    {
        // Using transform because you cannot get children as gameobjects
        Transform[] transforms = GetComponentsInChildren<Transform>();
        foreach (Transform childTransform in transforms)
        {
            ApplyModeDefaults(childTransform.gameObject);
        }
    }


    void ApplyModeDefaults(List<GameObject> gameObjects)
    {
        for (int i = 0; i < gameObjects.Count; i++)
        {
            ApplyModeDefaults(gameObjects[i]);
        }
    }


    void ApplyModeDefaults(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
        Renderer renderer = obj.GetComponent<Renderer>();
        switch (partMode)
        {

            case Mode.OutlinePart:
                if (collider != null)
                {
                    collider.isTrigger = true;
                    collider.enabled = true;
                }
                if (rigidbody != null)
                {
                    if (rigidbody != gameObject.GetComponent<Rigidbody>())
                    {
                        Destroy(rigidbody);
                    }
                    else
                    {
                        rigidbody.useGravity = false;
                        rigidbody.isKinematic = true;
                    }
                }
                if (renderer != null)
                {
                    renderer.materials = GetArrayOfMaterial(defaultOutlineMaterial, renderer.materials.Length);
                }
                break;
            case Mode.InteractablePart:
                if (collider != null)
                {
                    collider.isTrigger = false;
                    collider.enabled = true;
                }
                if (renderer != null)
                    renderer.materials = GetOriginalMaterials(renderer);
                break;
        }
    }


    private Dictionary<int, Material[]> SaveOriginalMaterials(List<GameObject> gameObjects)
    {
        Dictionary<int, Material[]> ogMaterials = new Dictionary<int, Material[]>();
        foreach (GameObject obj in gameObjects)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
                ogMaterials.Add(obj.GetInstanceID(), renderer.materials);
        }
        return ogMaterials;
    }


    private Dictionary<string, Collider> SaveOriginalColliders(List<GameObject> gameObjects)
    {
        Dictionary<string, Collider> ogColliders = new Dictionary<string, Collider>();
        foreach (GameObject obj in gameObjects)
        {
            Collider collider = obj.GetComponent<Collider>();
            ogColliders.Add(obj.name, collider);
        }
        return ogColliders;
    }


    private Dictionary<int, Rigidbody> SaveOriginalRigidbodies(List<GameObject> gameObjects)
    {
        Dictionary<int, Rigidbody> ogRigidbodies = new Dictionary<int, Rigidbody>();
        foreach (GameObject obj in gameObjects)
        {
            Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
            ogRigidbodies.Add(obj.GetInstanceID(), rigidbody);
        }
        return ogRigidbodies;
    }


    private void ApplyMaterialToList(List<GameObject> gameObjects, Material mat)
    {
        for (int i = 0; i < gameObjects.Count; i++)
        {
            Renderer renderer = gameObjects[i].GetComponent<Renderer>();
            if (renderer != null)
                renderer.materials = GetArrayOfMaterial(mat, renderer.materials.Length);
        }
    }


    private Material[] GetOriginalMaterials(Renderer renderer)
    {
        if (renderer != null)
        {
            int InstanceId = renderer.gameObject.GetInstanceID();
            if (originalMaterials.ContainsKey(InstanceId))
            {
                return originalMaterials[InstanceId];
            }

        }
        return null;
    }


    private Material[] GetArrayOfMaterial(Material mat, int size)
    {
        Material[] materials = new Material[size];
        for (int i = 0; i < size; i++)
        {
            materials[i] = mat;
        }
        return materials;
    }


    private List<GameObject> FindAllGameObjectsAtOrBelow(GameObject start)
    {
        List<GameObject> gameObjects = new List<GameObject>();
        FindAllGameObjectsAtOrBelow(gameObject, gameObjects);
        return gameObjects;
    }

    private void FindAllGameObjectsAtOrBelow(GameObject start, List<GameObject> objects)
    {
        objects.Add(start);
        Transform[] transforms = start.GetComponentsInChildren<Transform>();

        foreach (Transform childTransform in transforms)
        {
            if (childTransform.parent == start.transform)
                FindAllGameObjectsAtOrBelow(childTransform.gameObject, objects);
        }
    }

    private bool IsWithinRangeOfRotation(Quaternion rot1, Quaternion rot2, float limit)
    {
        // Debug.Log("Rotation between objects: " + Quaternion.Angle(rot1, rot2));
        return Quaternion.Angle(rot1, rot2) <= limit ? true : false;
    }

    private bool CollidersWithinLimit(Collider collider1, Collider collider2, float limit)
    {
        return Vector3.Distance(collider1.bounds.center, collider2.bounds.center) <= limit ? true : false;
    }

    private bool IsWithinRangeOfCenter(Transform otherTransform, float limit)
    {
        if (selfBoundsExpired)
        {
            selfGroupBounds = CalculateGroupedBounds(this.transform);
            selfBoundsExpired = false;
        }
        if (otherBoundsExpired)
        {
            otherGroupBounds = CalculateGroupedBounds(otherTransform);
            otherBoundsExpired = false;
        }
        return Vector3.Distance(selfGroupBounds.center, otherGroupBounds.center) <= limit ? true : false;
    }


    private Bounds CalculateGroupedBounds(Transform trans)
    {
        Renderer[] renderers = trans.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds; // Might need to check that renderer is null first
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }


    private void OnAcceptablePlacement()
    {
        onAcceptablePlacement.Invoke();
        // Release attached object
        // move to transform of the outline part
        // remove or hide the outline part
    }


    private void OnUnacceptablePlacement()
    {
        onUnacceptablePlacement.Invoke();
    }
}
