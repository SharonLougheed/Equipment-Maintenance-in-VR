using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionMode : MonoBehaviour {

    // NOTE Only partMode=OutlinePart and outlineIsTrigger=false is stable. The interactive object (eg. new part held by user) must be set to be a trigger right now
    /*
     * Part Mode states
     *  Unchanged: no changes will be made to this part
     *  BackgroundPart: a background part is not interactable and not does not have an active collider.
     *  BackgroundPartCollider: a background part is not interactable but does collide with interactable objects.
     *  InteractablePart: part is interactable.
     *  OutlinePart: Part is not collidable and by default has a translucent material to show where/how the part is oriented in an assembly.
     */
    public enum Mode { Unchanged, BackgroundPart, BackgroundPartCollider, OutlinePart, InteractablePart };
    public Mode partMode;
    [Tooltip("Amount of deviation from a perfect rotation match on all axes")]
    public bool checkRotation = true; // TODO only check if true
    public float acceptableDegrees = 10f;
    public bool checkPosition = true; // TODO only check if true
    public float acceptableMeters = 0.1f;
    public bool changeMode = false;
    [Tooltip("Trigger will be used instread of collision")]
    public bool outlineIsTrigger = false; // When using collision mode the interactive object must be a trigger
    public Material defaultOutlineMaterial;
    public Material acceptablePlacementMaterial;
    public Material unacceptablePlacementMaterial;
    private Dictionary<int, Material[]> originalMaterials;
    private Dictionary<int, Collider[]> originalColliders; // Only going to be used if outlineIsTrigger is set to true
    private Dictionary<int, Rigidbody> originalRigidbodies;
    private List<GameObject> allGameObjects;
    private bool isAcceptablePlacement = false;
    private int parentInstanceId;
    
	// Use this for initialization
	void Start () {
        parentInstanceId = GetInstanceID();
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
    void OnCollisionEnter(Collision collision)
    {
        Collider other = collision.collider;
        if (partMode != Mode.OutlinePart)
        {
            return;
        }

        if (isWithinRotationLimit(transform.rotation, other.transform.rotation, acceptableDegrees)
            && isWithinPositionLimit(transform, other.transform, acceptableMeters))
        {
            isAcceptablePlacement = true;
            ApplyMaterialToList(allGameObjects, acceptablePlacementMaterial);
        }
        else
        {
            ApplyMaterialToList(allGameObjects, unacceptablePlacementMaterial);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        Collider other = collision.collider;
        isAcceptablePlacement = false;
        ApplyMaterialToList(allGameObjects, defaultOutlineMaterial);
    }

    void OnTriggerEnter(Collider other)
    {
        if(partMode != Mode.OutlinePart)
        {
            return;
        }

        if (isWithinRotationLimit(transform.rotation, other.transform.rotation, acceptableDegrees)
            && isWithinPositionLimit(transform, other.transform, acceptableMeters))
        {
            isAcceptablePlacement = true;
            ApplyMaterialToList(allGameObjects, acceptablePlacementMaterial);
        }
        else
        {
            ApplyMaterialToList(allGameObjects, unacceptablePlacementMaterial);
        } 
    }

    void OnTriggerExit(Collider other)
    {
            isAcceptablePlacement = false;
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
                if (collider != null)
                {
                    collider.isTrigger = false;
                    collider.enabled = false;
                }
                if (rigidbody != null)
                {
                    rigidbody.useGravity = false;
                    rigidbody.isKinematic = true;
                }
                if(renderer != null)
                    renderer.materials = GetOriginalMaterials(renderer);
                break;
            case Mode.BackgroundPartCollider:
                if (collider != null)
                {
                    collider.isTrigger = false;
                    collider.enabled = false;
                }
                if (rigidbody != null)
                {
                    rigidbody.useGravity = false;
                    rigidbody.isKinematic = false;
                }
                if (renderer != null)
                    renderer.materials = GetOriginalMaterials(renderer);
                break;
            case Mode.OutlinePart:
                if (collider != null)
                {
                    collider.isTrigger = outlineIsTrigger;
                    collider.enabled = true;
                }
                if (rigidbody != null)
                {
                    if (rigidbody != gameObject.GetComponent<Rigidbody>())
                        Destroy(rigidbody);
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
                    collider.enabled = false;
                }
                if (rigidbody != null)
                {
                    rigidbody.useGravity = false;
                    rigidbody.isKinematic = true;
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

    private bool isWithinRotationLimit(Quaternion rot1, Quaternion rot2, float limit)
    {
        Debug.Log("Rotation between objects: " + Quaternion.Angle(rot1, rot2));
        return Quaternion.Angle(rot1, rot2) <= limit ? true : false;
    }

    private bool isWithinPositionLimit(Transform trans1, Transform trans2, float limit)
    {
        Debug.Log("Distance between objects: " + Vector3.Distance(trans1.position, trans2.position));
        return Vector3.Distance(trans1.position, trans2.position) <= limit ? true : false;
    }

    // TODO if object only has mesh collider create and add a primitive on using its bounds
    private Collider ApproximatePrimitiveCollider()
    {
        return null;
    }
}
