using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartnersAvatarManager : MonoBehaviour
{
    [SerializeField] private GameObject partnersHeadTarget;
    [SerializeField] private GameObject partnersLeftHandTarget;
    [SerializeField] private GameObject partnersRightHandTarget;

    [SerializeField] private GameObject partnersAvatar;

    private UsersPosture partnersPosture = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (partnersPosture is not null)
        {
            partnersAvatar.SetActive(true);

            MoveObject(partnersHeadTarget, partnersPosture.head);
            MoveObject(partnersLeftHandTarget, partnersPosture.leftHand);
            MoveObject(partnersRightHandTarget, partnersPosture.rightHand);
        }
    }

    public void SetPartnersPosture(UsersPosture partnersPosture)
    {
        this.partnersPosture = partnersPosture;
    }

    private void MoveObject(GameObject gameObject, Posture posture)
    {
        gameObject.transform.position = new Vector3(posture.position.x, posture.position.y, posture.position.z);
        gameObject.transform.rotation = Quaternion.Euler(posture.rotation);
    }
}
