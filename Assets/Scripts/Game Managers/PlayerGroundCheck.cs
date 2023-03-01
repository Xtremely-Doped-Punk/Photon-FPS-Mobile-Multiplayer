using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Task
{
	public class PlayerGroundCheck : MonoBehaviour
	{
		[SerializeField] GameObject ParentObjectController;
        private bool _grounded; 
		public bool isGrounded => _grounded;

		void OnTriggerEnter(Collider other)
		{
			if (other.gameObject == ParentObjectController)
				return;

			_grounded = true;
		}

		void OnTriggerExit(Collider other)
		{
			if (other.gameObject == ParentObjectController)
				return;

			_grounded = false;
		}

		void OnTriggerStay(Collider other)
		{
			if (other.gameObject == ParentObjectController)
				return;

			_grounded = true;
		}
	}
}