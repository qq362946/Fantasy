using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItem : MonoBehaviour
{
    public GameObject Item;

    private GameObject _currentItem;

    private float _spawnTime = 4;

    private void FixedUpdate()
    {
        if (!_currentItem)
        {
            _spawnTime -= Time.deltaTime;
            if (_spawnTime <= 0)
            {
                _spawnTime = 4;
                SpawnTheItem();
            }
        }
    }

    public void SpawnTheItem()
    {
        if (_currentItem)
        {
            Destroy(_currentItem);
        }

        if (Item)
        {
            _currentItem = Instantiate(Item, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning("Please add a item to spawn.");
        }
    }
}