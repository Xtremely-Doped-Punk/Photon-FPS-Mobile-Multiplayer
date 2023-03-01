using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Task;
using UnityEngine;

public class RandomizeImpact : MonoBehaviour
{
    private static RandomizeImpact Instance;
    [SerializeField] private Texture[] textures;
    [SerializeField] private Material RefMaterial;

    private void Awake()
    {
        Instance = this;
    }

    public static Material GetRandomMaterial()
    {
        return Instance.GenerateRandomMaterial();
    }
    private Material GenerateRandomMaterial()
    {
        var tex = Instance.textures[Random.Range(0, Instance.textures.Length)];
        var temp_material = new Material(RefMaterial);
        temp_material.SetTexture("_BaseMap", tex);
        return temp_material;
    }
}
