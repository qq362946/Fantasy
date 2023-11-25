using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Inputs : MonoBehaviour
{
    public abstract float GetHorizontal();

    public abstract float GetVertical();

    public abstract bool Jump();

    public abstract bool Dash();

    public abstract bool JetPack();

    public abstract bool Parachute();
    public abstract bool DropCarryItem();
}