using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterControllerHelper : ControllerHelper<CharacterController>
{
  public CharacterControllerHelper(CharacterController controller, Inventory inventory) : base(controller, inventory) { }
}