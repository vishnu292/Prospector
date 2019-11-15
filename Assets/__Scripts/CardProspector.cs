using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum eCardstate {
    drawpile,
    tableau,
    target,
    discard
}

public class CardProspector : Card
{
    // Start is called before the first frame update
    [Header("Set Dynamically: CardProspector")]
    public eCardstate state = eCardstate.drawpile;
    public List<CardProspector> hiddenBy = new List<CardProspector>();
    public int layoutID;
    public SlotDef slotDef;

    public override void OnMouseUpAsButton()
    {
        Prospector.S.CardClicked(this);
        base.OnMouseUpAsButton();
    }


}
