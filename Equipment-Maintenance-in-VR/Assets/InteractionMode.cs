using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionMode : MonoBehaviour {

    /*
     *  Mode states
     *  Unchanged: no changes will be made to this part
     *  BackgroundPart: a background part is not interactable and not does not have an active collider.
     *  BackgroundPartCollider: a background part is not interactable but does collide with interactable objects.
     *  InteractablePart: part is interactable.
     *  OutlinePart: Part is not collidable and by default has a translucent material to show where/how the part is oriented in an assembly.
     */
    public enum Mode { Unchanged, BackgroundPart, BackgroundPartCollider, OutlinePart, InteractablePart };
    public Mode partMode;
    public float acceptableDegrees = 10f; // TODO tooltip for what this does
    public bool changeMode = false;
    public Material defaultOutlineMaterial;
    public Material acceptablePlacementMaterial;
    private Dictionary<string, Material[]> originalMaterials;
    private List<GameObject> allGameObjects;
    private bool isAcceptablePlacement = false;
	// Use this for initialization
	void Start () {
        if(defaultOutlineMaterial == null)
        {
            defaultOutlineMaterial = Resources.Load("Materials/OutlineMatOrange") as Material;
        }
        if (acceptablePlacementMaterial == null)
        {
            acceptablePlacementMaterial = Resources.Load("Materials/OutlineMatGreen") as Material;
        }
        allGameObjects = FindAllGameObjectsAtOrBelow(gameObject);
        originalMaterials = SaveOriginalMaterials(allGameObjects);

        //ApplyModeDefaultsToSelf();
        //ApplyModeDefaultsToChildren();
        ApplyModeDefaults(allGameObjects);
    }
	
	// Update is called once per frame
	void Update () {
        if (changeMode)
        {
            ApplyModeDefaultsToSelf();
            ApplyModeDefaultsToChildren();
            changeMode = false;
        }
	}
    
    void OnTriggerEnter(Collider other)
    {
        if(partMode != Mode.OutlinePart)
        {
            return;
        }

        float angle = Quaternion.Angle(transform.rotation, other.transform.rotation);
        if (angle <= acceptableDegrees)
        {
            isAcceptablePlacement = true;
            print("Entering acceptable placement. " + "Rotation: " + angle);

        }
        else
        {
            print("Entering UNACCEPTABLE placement. " + "Rotation: " + angle);
        }
      
    }

    void OnTriggerExit(Collider other)
    {
        if (isAcceptablePlacement)
        {
            print("Leaving acceptable placement");
            isAcceptablePlacement = false;
        }
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
    void ApplyModeDefaults(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        Renderer renderer = gameObject.GetComponent<Renderer>();
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
                    collider.isTrigger = true;
                    collider.enabled = false;
                }
                if (rigidbody != null)
                {
                    rigidbody.useGravity = false;
                    rigidbody.isKinematic = true;
                }
                if (renderer != null)
                {
                    //print(gameObject.name + " materials: " + renderer.materials.Length);
                    //Material[] changedMaterials = new Material[renderer.materials.Length];
                    //for (int i = 0; i < renderer.materials.Length; i++)
                    //{
                    //    changedMaterials[i] = defaultOutlineMaterial;
                    //}
                    //renderer.materials = changedMaterials;
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

    private Dictionary<string, Material[]> SaveOriginalMaterials(List<GameObject> gameObjects)
    {
        Dictionary<string, Material[]> ogMaterials = new Dictionary<string, Material[]>();
        foreach(GameObject obj in gameObjects)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
                ogMaterials.Add(obj.GetInstanceID().ToString(), renderer.materials);
        }
        return ogMaterials;
    }

    // TODO Recursively store original materials
    //private Dictionary<string, Material[]> SetOriginalMaterials()
    //{
    //    Dictionary<string, Material[]> ogMaterials = new Dictionary<string, Material[]>();
    //    Renderer renderer = GetComponent<Renderer>();
    //    if(renderer != null)
    //        ogMaterials.Add(gameObject.GetInstanceID().ToString(), renderer.materials);

    //    foreach(Renderer childRenderer in GetComponentsInChildren<Renderer>())
    //    {
    //        if(childRenderer != null)
    //            ogMaterials.Add(childRenderer.gameObject.GetInstanceID().ToString(), childRenderer.materials);
    //    }

    //    return ogMaterials;
    //}

    private Material[] GetOriginalMaterials(Renderer renderer)
    {
        if(renderer != null)
        {
            string InstanceId = renderer.gameObject.GetInstanceID().ToString();
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
        print("Name: " + start.name + ", size of all objects: " + gameObjects.Count);
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

    // TODO if object only has mesh collider create and add a primitive on using its bounds
    private Collider ApproximatePrimitiveCollider()
    {
        return null;
    }
}
