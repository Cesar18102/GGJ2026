using Assets.Scripts.Input;
using System.Collections;
using UnityEngine;

public abstract class GameActionHandler : MonoBehaviour
{
    public abstract IEnumerator WaitForStart();
    public abstract void Execute(InputData input);
    public abstract bool CanExecute(InputData input);
    public abstract IEnumerator WaitForEnd();
}