
using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameTools.UISystem
{
    public static partial class UIManager
    {

        private struct Layer 
        {
            public readonly string name;
            public readonly int sortOrder;
            public readonly ContainerBase container;
            public Layer(string name, int sortOrder, ContainerBase container)
            {
                this.name = name;
                this.sortOrder = sortOrder;
                this.container = container;
            }
        }

        private static List<Layer> layers = new();

        /*public static void RegisterLayer(string name, int layerOrder, ContainerBase container)
        {
            if (layers.Exists(x => x.name == name)) throw new ArgumentException($"name: {name} already exists");
            int index = layers.FindLastIndex(item => item.sortOrder < layerOrder)+1;
            layers.Insert(index, (name, layerOrder, container));
        }*/

        public static TContainer AddLayer<TContainer>(string name, int layerOrder) where TContainer : ContainerBase, new()
        {
            if (layers.Exists(x => x.name == name)) throw new ArgumentException($"name: {name} already exists");
            int index = layers.FindLastIndex(item => item.sortOrder < layerOrder)+1;
            TContainer container = new();
            layers.Insert(index, new Layer(name, layerOrder, container));
            return container;
        }

        public static TContainer GetContainer<TContainer>(string name) where TContainer : ContainerBase, new()
        {
            int index = layers.FindIndex(item => item.name == name);
            if (index<0)
            {
                Debug.LogError($"name: {name} not found");
                return null;
            }
            var containerBase =  layers[index].container;
            if (containerBase is TContainer container)  return container;
            Debug.LogError($"type: {typeof(TContainer)} not match with type: {containerBase.GetType()}");
            return null;
        }
        
        private static int callDelayCount;
        private static bool orderDirty, interactableDirty;
        public static void DelayStateUpdate(Action action)
        {
            try
            {
                callDelayCount++;
                action?.Invoke();
            }
            finally
            {
                callDelayCount--;
                if (callDelayCount == 0)
                {
                    if(orderDirty) UpdateOrderImmediate();
                    if(interactableDirty) UpdateInteractableImmediate();
                }
            }
        }
        
        public static IDisposable DelayStateUpdateScope()
        {
            return new DelayScope();
        }

        private sealed class DelayScope : IDisposable
        {
            private bool disposed;
            public DelayScope()
            {
                callDelayCount++;
            }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                callDelayCount--;
                if (callDelayCount > 0) return;
                if(orderDirty) UpdateOrderImmediate();
                if(interactableDirty) UpdateInteractableImmediate();
            }
        }

        public static void UpdateOrder()
        {
            if (callDelayCount > 0) 
            { 
                orderDirty = true; 
                return; 
            }
            UpdateOrderImmediate();
        }

        public static void UpdateInteractable()
        {
            if (callDelayCount > 0)
            {
                interactableDirty = true;
                return;
            }
            UpdateInteractableImmediate();
        }

        public static void UpdateOrderImmediate()
        {
            int order = 0;
            foreach (var item in layers)
            {
                item.container.UpdateOrder(ref order);
            }
            orderDirty =false;
        }

        public static void UpdateInteractableImmediate()
        { 
            bool interactable = true;
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                layers[i].container.UpdateInteractable(ref interactable);
            }
            interactableDirty = false;
        }
    }
}