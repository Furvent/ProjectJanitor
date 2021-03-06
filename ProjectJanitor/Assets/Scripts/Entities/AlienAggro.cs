using UnityEngine;

namespace GalacticJanitor.Game
{
    [RequireComponent(typeof(SphereCollider))]
    public class AlienAggro : MonoBehaviour
    {

        public AlienBase alien;

        void Start() { }

        void OnTriggerEnter(Collider other)
        {
            if (alien == null) return;

            if (other.gameObject.tag == "Player")
                OnAggro(other.transform);

        }

        void OnAggro(Transform trackableTarget)
        {
            if (alien.target == null)
            {
                alien.SetTarget(trackableTarget);

                /*SOUND*/
                //if (alien.onAggroSound) alien.onAggroSound.Play();
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (alien == null) return;

            if (alien.target == null && other.gameObject.tag == "Player")
            {
                OnAggro(other.transform);
            }

            if (alien.target != null && other.gameObject.tag == "Alien")
            {
                AlienBase spreaded = other.gameObject.GetComponent<AlienBase>();
                if (spreaded.target == null)
                {
                    spreaded.SetTarget(alien.target);
                }
            }
        }
    } 
}
