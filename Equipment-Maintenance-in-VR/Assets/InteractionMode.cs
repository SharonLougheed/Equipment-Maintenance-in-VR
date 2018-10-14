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
    public bool changeMode = false;
    public Material defaultOutlineMaterial;
    private Dictionary<string, Material[]> originalMaterials;
    

	// Use this for initialization
	void Start () {
        if(defaultOutlineMaterial == null)
        {
            defaultOutlineMaterial = Resources.Load("Materials/OutlineMatOrange") as Material;
        }
        originalMaterials = SetOriginalMaterials();
        
        ApplyModeDefaultsToSelf();
        ApplyModeDefaultsToChildren();
        
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

    void ApplyModeDefaults(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        Renderer renderer = gameObject.GetComponent<Renderer>();
        switch (partMode)
        {
            case Mode.BackgroundPart:
                if (collider != null)
                    collider.enabled = false;
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
                    collider.enabled = true;
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
                    collider.enabled = false;
                if (rigidbody != null)
                {
                    rigidbody.useGravity = false;
                    rigidbody.isKinematic = true;
                }
                if (renderer != null)
                {
                    Material[] changedMaterials = new Material[renderer.materials.Length];
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        changedMaterials[i] = defaultOutlineMaterial;
                    }
                    renderer.materials = changedMaterials;
                }


                break;
            case Mode.InteractablePart:
                if (collider != null)
                    collider.enabled = true;
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

    private Dictionary<string, Material[]> SetOriginalMaterials()
    {
        Dictionary<string, Material[]> ogMaterials = new Dictionary<string, Material[]>();
        Renderer renderer = GetComponent<Renderer>();
        if(renderer != null)
            ogMaterials.Add(gameObject.GetInstanceID().ToString(), renderer.materials);

        foreach(Renderer childRenderer in GetComponentsInChildren<Renderer>())
        {
            if(childRenderer != null)
                ogMaterials.Add(childRenderer.gameObject.GetInstanceID().ToString(), childRenderer.materials);
        }

        return ogMaterials;
    }

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
}
