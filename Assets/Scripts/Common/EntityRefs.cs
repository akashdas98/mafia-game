using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class EntityRefs : MonoBehaviour
{
  [SerializeField] private List<Component> parts = new List<Component>();

  private readonly Dictionary<Type, List<Component>> lookup = new Dictionary<Type, List<Component>>();

  public IReadOnlyList<Component> Parts => parts;

  private void OnValidate()
  {
    Rebuild();
  }

  private void OnEnable()
  {
    Rebuild();
  }

  private void OnTransformChildrenChanged()
  {
    Rebuild();
  }

  private void Awake()
  {
    Rebuild();
  }

  [ContextMenu("Rebuild Parts")]
  public void Rebuild()
  {
    parts.Clear();
    lookup.Clear();

    Component[] components = GetComponentsInChildren<Component>(true);
    for (int i = 0; i < components.Length; i++)
    {
      Component component = components[i];
      if (component == null || component == this || component is Transform)
      {
        continue;
      }

      parts.Add(component);
      RegisterTypeHierarchy(component);

      Type[] interfaces = component.GetType().GetInterfaces();
      for (int j = 0; j < interfaces.Length; j++)
      {
        Register(interfaces[j], component);
      }
    }
  }

  public bool TryGet<T>(out T part) where T : class
  {
    EnsureLookup();
    if (lookup.TryGetValue(typeof(T), out List<Component> matches))
    {
      for (int i = 0; i < matches.Count; i++)
      {
        if (matches[i] is T typed)
        {
          part = typed;
          return true;
        }
      }
    }

    part = null;
    return false;
  }

  public T Get<T>() where T : class
  {
    TryGet(out T part);
    return part;
  }

  public List<T> GetAll<T>() where T : class
  {
    EnsureLookup();
    List<T> results = new List<T>();
    if (lookup.TryGetValue(typeof(T), out List<Component> matches))
    {
      for (int i = 0; i < matches.Count; i++)
      {
        if (matches[i] is T typed)
        {
          results.Add(typed);
        }
      }
    }

    return results;
  }

  private void Register(Type type, Component component)
  {
    if (!lookup.TryGetValue(type, out List<Component> matches))
    {
      matches = new List<Component>();
      lookup[type] = matches;
    }

    if (!matches.Contains(component))
    {
      matches.Add(component);
    }
  }

  private void RegisterTypeHierarchy(Component component)
  {
    Type type = component.GetType();
    while (type != null && type != typeof(Component) && type != typeof(MonoBehaviour))
    {
      Register(type, component);
      type = type.BaseType;
    }
  }

  private void EnsureLookup()
  {
    if (lookup.Count == 0 || lookup.Count < parts.Count)
    {
      Rebuild();
    }
  }
}
