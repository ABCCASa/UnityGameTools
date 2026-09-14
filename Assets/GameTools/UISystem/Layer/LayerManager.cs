using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace GameTools.UISystem
{
    public static class LayerManager
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

        private static readonly List<Layer> layers = new();

        public static TContainer AddContainer<TContainer>(int layerOrder, string name = null, IScreenLoader screenLoader = null) where TContainer : ContainerBase, new()
        {
            if (name != null && layers.Exists(x => x.name == name)) throw new ArgumentException($"name: {name} already exists");
            int index = layers.FindLastIndex(item => item.sortOrder <= layerOrder) + 1;
            TContainer container = new();
            container.Initialize(screenLoader);
            layers.Insert(index, new Layer(name, layerOrder, container));
            return container;
        }

        private static int FindLayerIndex(string name)
        {
            if (name == null)
            {
                Debug.LogError("匿名 layer 无法通过 name 查询");
                return -1; 
            }
            int index = layers.FindIndex(item => item.name == name);
            if(index < 0) Debug.LogError($"name: {name} not found");
            return index; 
        }
        
        private static ContainerBase GetContainer(string name)
        {
            int index = FindLayerIndex(name);
            return index >= 0 ? layers[index].container : null;
        }
        
        public static TContainer GetContainer<TContainer>(string name) where TContainer : ContainerBase, new()
        {
            ContainerBase containerBase = GetContainer(name);
            if (containerBase == null) return null;
            if (containerBase is TContainer container) return container;
            Debug.LogError($"name: {name}, type: {typeof(TContainer)} does not match, expected type: {containerBase.GetType()}");
            return null;
        }

        private static void RemoveContainer(int index)
        {
            if (index == -1) return; 
            ContainerBase container = layers[index].container;
            container.Dispose();
            Assert.IsTrue(container.count == 0, $"{container} is not empty");
            index = layers.FindIndex(item => item.container == container);// get index again
            layers.RemoveAt(index);
        }

        public static void RemoveContainer(string name)
        {
            int index = FindLayerIndex(name);
            RemoveContainer(index);
        }

        public static void RemoveContainer(ContainerBase container)
        {
            int index = layers.FindLastIndex(item => item.container == container);
            if (index == -1) throw new ArgumentException($"container: {container} does not exist");
            RemoveContainer(index);
        }


        public static void ChangeOrder(ScreenContainer container, int newOrder)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            int oldIndex = layers.FindIndex((l) => l.container == container);
            if (oldIndex == -1) throw new Exception($"{container} not found");
            Layer oldLayer = layers[oldIndex];
            layers.RemoveAt(oldIndex);
            int newIndex = layers.FindLastIndex(item => item.sortOrder <= newOrder) + 1;
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
            ContainerBase container = oldLayer.container;
            layers.RemoveAt(oldIndex);
            int newIndex = layers.FindLastIndex(item => item.sortOrder <= newOrder) + 1;
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
                if (orderDirty) UpdateOrderImmediate();
                if (interactableDirty) UpdateInteractableImmediate();
            }
        }

        public static void UpdateOrder()
        {
            if (callDelayCount > 0) orderDirty = true;
            else UpdateOrderImmediate();
        }

        public static void UpdateInteractable()
        {
            if (callDelayCount > 0) interactableDirty = true;
            else UpdateInteractableImmediate();
        }

        public static void UpdateOrderImmediate()
        {
            int order = 0;
            foreach (var item in layers)
            {
                item.container.UpdateOrder(ref order);
            }

            orderDirty = false;
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