using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;


public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex
    {
        get;
        set;
    }
}
public class PriorityQueue<T> where T : IHeapItem<T>
{
    private T[] items;
    private int itemCount;

    public PriorityQueue(int initialSize)
    {
        items = new T[initialSize];
        itemCount = 0;
    }
    public T Peek()
    {
        return items[0];
    }
    public void UpdateItem(T item)
    {
        HeapifyUp(item);
    }
    public void Add(T item)
    {
        item.HeapIndex = itemCount;
        items[itemCount] = item;
        HeapifyUp(item);
        itemCount++;
    }
    public T Pop()
    {
        T item = items[0];
        itemCount--;
        items[0] = items[itemCount];
        items[0].HeapIndex = 0;

        HeapifyDown(items[0]);

        return item;
    }
    private void HeapifyUp(T item)
    {
        int parentIndex;
        while (item.HeapIndex > 0)
        {
            parentIndex = (item.HeapIndex - 1) / 2;
            T parent = items[parentIndex];
            if (item.CompareTo(parent) > 0)
            {
                Swap(item, parent);
            }
            else return;
        }
    }
    private void HeapifyDown(T item)
    {
        while (true)
        {
            int leftChildIndex = item.HeapIndex * 2 + 1;
            int rightChildIndex = item.HeapIndex * 2 + 2;
            int swapIndex = 0;

            if (leftChildIndex >= itemCount)
                break;

            swapIndex = leftChildIndex;

            if (rightChildIndex < itemCount)
            {
                if (items[leftChildIndex].CompareTo(items[rightChildIndex]) < 0)
                {
                    swapIndex = rightChildIndex;
                }

            }

            if (item.CompareTo(items[swapIndex]) < 0)
            {
                Swap(item, items[swapIndex]);
            }
            else
            {
                break;
            }

        }
    }

    public int Count
    {
        get
        {
            return itemCount;
        }
    }
    public bool Contains(T item)
    {
        if (item.HeapIndex < 0 || item.HeapIndex >= itemCount) return false;
        return Equals(items[item.HeapIndex], item);
    }
    private void Swap(T a, T b)
    {
        items[a.HeapIndex] = b;
        items[b.HeapIndex] = a;
        int aIndex = a.HeapIndex;
        a.HeapIndex = b.HeapIndex;
        b.HeapIndex = aIndex;
    }
}
