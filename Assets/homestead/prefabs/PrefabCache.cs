﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using RedHomestead.Interiors;
using System;
using RedHomestead.Geography;

public static class PrefabCacheUtils
{
    public static Material TranslucentValidPlanningMaterial;
    public static Material TranslucentInvalidPlanningMaterial;

    public static void RecurseDisableColliderSetTranslucentRenderer(Transform parent, Material translucent)
    {
        foreach (Transform child in parent)
        {
            Collider c = child.GetComponent<Collider>();
            if (c != null)
                c.enabled = false;

            Renderer r = child.GetComponent<Renderer>();

            if (r != null)
            {
                r.enabled = (child.gameObject.layer == 0) || (child.gameObject.layer == 8);

                if (r.enabled)
                {
                    if (r.materials != null && r.materials.Length > 1)
                    {
                        var newMats = new Material[r.materials.Length];
                        for (int i = 0; i < r.materials.Length; i++)
                        {
                            newMats[i] = translucent;
                        }
                        r.materials = newMats;
                    }
                    else
                    {
                        r.material = translucent;
                    }
                }
            }
            Light l = child.GetComponent<Light>();
            if (l != null)
                l.enabled = false;

            RecurseDisableColliderSetTranslucentRenderer(child, translucent);
        }
    }
}

public class PrefabCache<T> where T : IConvertible {
    private static PrefabCache<T> _cache;
    public static PrefabCache<T> Cache
    {
        get
        {
            if (_cache == null)
                _cache = new PrefabCache<T>();

            return _cache;
        }
    }

    /// <summary>
    /// Visualization == transparent preview of module to be built
    /// Cache == only create 1 of each type of module because creation is expensive
    /// </summary>
    private Dictionary<T, Transform> VisualizationTransformCache = new Dictionary<T, Transform>();

    public Transform GetVisualization(T key, bool translucent = false)
    {
        Transform result = null;

        if (VisualizationTransformCache.ContainsKey(key))
        {
            result = VisualizationTransformCache[key];
            result.gameObject.SetActive(true);
        }
        else
        {
            result = GameObject.Instantiate<Transform>(GetPrefab(key));
            if (typeof(T) == typeof(Module))
            {
                ModuleGameplay g = result.GetComponent<ModuleGameplay>();
                if (g != null)
                    GameObject.Destroy(g);
            }
            PrefabCacheUtils.RecurseDisableColliderSetTranslucentRenderer(result, PrefabCacheUtils.TranslucentValidPlanningMaterial);
            VisualizationTransformCache[key] = result;
        }

        return result;
    }
    
    
    public Transform GetPrefab(T key)
    {
        if (typeof(T) == typeof(Module))
        {
            return ModuleBridge.Instance.Modules[GetIndex(key)];
        }
        if (typeof(T) == typeof(Stuff))
        {
            return FloorplanBridge.Instance.StuffFields.Prefabs[GetIndex(key)];
        }
        if (typeof(T) == typeof(Floorplan))
        {
            return FloorplanBridge.Instance.GetPrefab((Floorplan)Enum.Parse(typeof(Floorplan), key.ToString()));
        }
        else
            return null;
    }

    private int GetIndex(T key)
    {
        Enum myEnum = Enum.Parse(typeof(T), key.ToString()) as Enum;
        return Convert.ToInt32(myEnum);
    }

    public void Clear()
    {
        VisualizationTransformCache.Clear();
    }
}

public class Planning<T> where T : IConvertible
{
    public Transform Visualization { get; private set; }
    public T Type { get; private set; }
    private bool _isValid = true;
    public bool IsValid
    {
        get { return _isValid; }
        set
        {
            if (_isValid != value)
            {
                SetValidMaterialOnVisualization(value);
            }
            _isValid = value;
        }
    }
    public bool IsActive
    {
        get
        {
            return Visualization != null;
        }
    }

    public void SetVisualization(T type)
    {
        this.Visualization = PrefabCache<T>.Cache.GetVisualization(type);
        this.Type = type;
    }

    public void Rotate(bool clockwise, bool smooth = true, int increment = 90)
    {
        float amount = (clockwise ? increment : -increment);

        if (smooth)
            amount *= Time.deltaTime;

        Visualization.Rotate(Vector3.up * amount);
    }

    public void Reset()
    {
        if (Visualization != null)
        {
            Visualization.gameObject.SetActive(false);
            Visualization = null;
        }
    }

    private void SetValidMaterialOnVisualization(bool isValid)
    {
        PrefabCacheUtils.RecurseDisableColliderSetTranslucentRenderer(Visualization, isValid ? PrefabCacheUtils.TranslucentValidPlanningMaterial : PrefabCacheUtils.TranslucentInvalidPlanningMaterial);
    }
}
