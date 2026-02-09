using UnityEngine;

public interface IInteractable
{
    float InteractRange { get; }//상호작용 거리

    void Interact(Transform interactor);//상호작용 로직
}
