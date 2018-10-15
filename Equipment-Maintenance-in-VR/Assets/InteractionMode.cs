using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionMode : MonoBehaviour {

    /* Not stable
     * Only method of triggering is having OutlinePart set to isTrigger
     * Need to store self bounds so it doesnt calculate it nearly every frame, easy fix
     * Need to find way to check if other object is replacement part (tag, is prefab, etc)
     * Current bug: not all children of OutlinePart are getting isTrigger set to true. Manual fix: set value for all chilren in editor before runtime
     */

    /*
     * Part Mode states
     *  Unchanged: no changes will be made to this part
     *  BackgroundPart: a background part is not interactable and not does not have an active collider.
     *  BackgroundPartCollider: a background part is not interactable but does collide with interactable objects.
     *  InteractablePart: part is interactable with an OutlinePart.
     *  OutlinePart: Part is not collidable and by default has a translucent material to show where/how the part is oriented in an assembly.
     */
    public enum Mode { Unchanged, BackgroundPart, BackgroundPartCollider, OutlinePart, InteractablePart };
    public Mode partMode;
    [Tooltip("Amount of deviation from a perfect rotation match on all axes")]
    public bool checkRotation = true;
    public float acceptableDegrees = 10f;
    public bool checkPosition = false;
    public float acceptableMeters = 1.0f;
    public bool changeMode = false;
    public Material defaultOutlineMaterial;
    public Material acceptablePlacementMaterial;
    public Material unacceptablePlacementMaterial;
    private Dictionary<int, Material[]> originalMaterials;
    private Dictionary<int, Collider[]> originalColliders; // Only going to be used if outlineIsTrigger is set to true
    private Dictionary<int, Rigidbody> originalRigidbodies;
    private List<GameObject> allGameObjects;
    // private bool isAcceptablePlacement = false;
    private bool isAcceptableRotation = false;
    private bool isAcceptablePosition = false;

    // Use this for initialization
    void Start () {
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
        allGameObjects = FindAllGameObjectsAtOrBelow(gameObject);
        originalMaterials = SaveOriginalMaterials(allGameObjects);
        originalColliders = SaveOriginalColliders(allGameObjects);
        originalRigidbodies = SaveOriginalRigidbodies(allGameObjects);

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
	void Update () {
        if (changeMode)
        {
            allGameObjects = FindAllGameObjectsAtOrBelow(gameObject);
            ApplyModeDefaults(allGameObjects);
            changeMode = false;
        }
	}
   
    void OnTriggerEnter(Collider other)
    {
        Debug.Log(name + " OnTriggerEnter");
    }

    void OnTriggerStay(Collider other)
    {
        Debug.Log(name + " OnTriggerStay");
        if (partMode != Mode.OutlinePart)
        {
            return;
        }

        if (!checkRotation || IsWithinRangeOfRotation(transform.rotation, other.transform.rotation, acceptableDegrees))
        {
            isAcceptableRotation = true;
        }
        if(!checkPosition || IsWithinRangeOfCenter(transform, other.transform, acceptableMeters))
        {
            isAcceptablePosition = true;
        }
        Debug.Log("IsAcceptableRotation: " + isAcceptableRotation + " IsAcceptablePosition: " + isAcceptablePosition);
        if((!checkRotation || isAcceptableRotation) && (!checkPosition || isAcceptablePosition))
        {
            ApplyMaterialToList(allGameObjects, acceptablePlacementMaterial);
        }
        else
        {
            ApplyMaterialToList(allGameObjects, unacceptablePlacementMaterial);
        }
        isAcceptableRotation = false;
        isAcceptablePosition = false;
    }

    void OnTriggerExit(Collider other)
    {
        isAcceptableRotation = false;
        isAcceptablePosition = false;
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
        foreach(Transform childTransform in transforms)
        {
            ApplyModeDefaults(childTransform.gameObject);
        }
    }

    void ApplyModeDefaults(List<GameObject> gameObjects)
    {
        Debug.Log("Total objects: " + gameObjects.Count);
        for(int i = 0; i < gameObjects.Count; i++)
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
            case Mode.BackgroundPart:

                break;
            case Mode.BackgroundPartCollider:

                break;
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
        foreach(GameObject obj in gameObjects)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
                ogMaterials.Add(obj.GetInstanceID(), renderer.materials);
        }
        return ogMaterials;
    }

    private Dictionary<int, Collider[]> SaveOriginalColliders(List<GameObject> gameObjects)
    {
        Dictionary<int, Collider[]> ogColliders = new Dictionary<int, Collider[]>();
        foreach (GameObject obj in gameObjects)
        {
            Collider[] colliders = obj.GetComponents<Collider>();
            ogColliders.Add(obj.GetInstanceID(), colliders);
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

    private void ApplyCollidersToParent()
    {
        foreach(Collider[] objColliders in originalColliders.Values)
        {
            foreach(Collider collider in objColliders)
            {
                // If collider is a primitive collider then add it to the parent
                // If mesh collider then create closest primitive box collider and add that to parent
            }
        }
    }

    private void ApplyMaterialToList(List<GameObject> gameObjects, Material mat)
    {
        for (int i = 0; i < gameObjects.Count; i++)
        {
            Renderer renderer = gameObjects[i].GetComponent<Renderer>();
            if(renderer != null)
                renderer.materials = GetArrayOfMaterial(mat, renderer.materials.Length);
        }
    }

    private Material[] GetOriginalMaterials(Renderer renderer)
    {
        if(renderer != null)
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
            if(childTransform.parent == start.transform)
                FindAllGameObjectsAtOrBelow(childTransform.gameObject, objects);
        }
    }

    private bool IsWithinRangeOfRotation(Quaternion rot1, Quaternion rot2, float limit)
    {
        Debug.Log("Rotation between objects: " + Quaternion.Angle(rot1, rot2));
        return Quaternion.Angle(rot1, rot2) <= limit ? true : false;
    }

    private bool IsWithinRangeOfCenter(Transform trans1, Transform trans2, float limit)
    {
        Bounds bounds1 = CalculateGroupedBounds(trans1);
        Bounds bounds2 = CalculateGroupedBounds(trans2);
        Debug.Log("Distance between objects: " + Vector3.Distance(bounds1.center, bounds2.center));
        return Vector3.Distance(bounds1.center, bounds2.center) <= limit ? true : false;
    }


    private Bounds CalculateGroupedBounds(Transform trans)
    {
        Renderer[] renderers = trans.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds; // Might need to check that renderer is null first
        foreach(Renderer renderer in renderers)
        {
            if(renderer != null)
                bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }
}
