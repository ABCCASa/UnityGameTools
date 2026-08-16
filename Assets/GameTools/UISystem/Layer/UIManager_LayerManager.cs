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
            public readonly ScreenContainer container;
            public Layer(string name, int sortOrder, ScreenContainer container)
            {
                this.name = name;
                this.sortOrder = sortOrder;
                this.container = container;
            }
        }
        private static readonly List<Layer> layers = new();
        
        public static ScreenContainer AddContainer(int layerOrder, string name = null)
        {
            if (name != null && layers.Exists(x => x.name == name)) throw new ArgumentException($"name: {name} already exists");
            int index = layers.FindLastIndex(item => item.sortOrder <= layerOrder)+1;
            ScreenContainer container = new ScreenContainer();
            layers.Insert(index, new Layer(name, layerOrder, container));
            return container;
        }
        
        internal static void AddContainer(ScreenContainer container, int layerOrder)
        {
            if(layers.Exists(x => x.container == container)) throw new ArgumentException($"{container} already exists");
            int index = layers.FindLastIndex(item => item.sortOrder <= layerOrder)+1;
            layers.Insert(index, new Layer(null, layerOrder, container));
            if (container.count == 0) return;
            UpdateOrder();
            UpdateInteractable();
        }

        internal static void RemoveContainer(ScreenContainer container)
        {
            if(container.count != 0) throw new ArgumentException($"{container} is not empty"); 
            int index = layers.FindLastIndex(item => item.container == container);
            if(index == -1) throw new ArgumentException($"container: {container} does not exist");
            layers.RemoveAt(index);
        }

        public static ScreenContainer GetContainer(string name)
        {
            if (name == null) throw new ArgumentNullException($"{nameof(name)}为null的是匿名layer，你无法查询它");
            int index = layers.FindIndex(item => item.name == name);
            if (index >= 0) return layers[index].container;
            Debug.LogError($"name: {name} not found");
            return null;
        }

        public static void ChangeOrder(ScreenContainer container, int newOrder)
        {
            if(container== null) throw new ArgumentNullException(nameof(container));
           int oldIndex = layers.FindIndex((l) => l.container == container);
           if (oldIndex == -1) throw new Exception($"{container} not found");
           Layer oldLayer = layers[oldIndex];
           layers.RemoveAt(oldIndex);
           
           int newIndex = layers.FindLastIndex(item => item.sortOrder <= newOrder)+1;
           layers.Insert(newIndex, new Layer(oldLayer.name, newOrder, container));
           if (container.count == 0) return;
           UpdateOrder();
           UpdateInteractable();
        }
        
        public static void ChangeOrder(string name, int newOrder)
        {
            if (name == null) throw new ArgumentNullException($"{nameof(name)}为null的是匿名layer，你无法查询它");
            int oldIndex = layers.FindIndex((l) => l.name == name);
            if (oldIndex == -1) throw new Exception($"name: {name} not found");
            Layer oldLayer = layers[oldIndex];
            ScreenContainer container = oldLayer.container;
            layers.RemoveAt(oldIndex);
            int newIndex = layers.FindLastIndex(item => item.sortOrder <= newOrder)+1;
            layers.Insert(newIndex, new Layer(oldLayer.name, newOrder, container));
            if (container.count == 0) return;
            UpdateOrder();
            UpdateInteractable();
        }

        private static int callDelayCount;
        private static bool orderDirty, interactableDirty;
        internal static IDisposable GetDelayScope() => new DelayScope();
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