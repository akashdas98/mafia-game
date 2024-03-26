using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterInputHandlerHelper : InputHandlerHelper<CharacterInputHandler>
{
  public CharacterInputHandlerHelper(CharacterInputHandler inputHandler) : base(inputHandler) { }
}