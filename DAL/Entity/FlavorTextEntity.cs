using System;
using System.Collections.Generic;
using System.Text;

namespace konlulu.DAL.Entity
{
    public enum FlavorTextIdentifyer
    {
        GameInit,
        GameRegister,
        GameStart,
        DollPass,
        Offer,

        NoInitiatingGame,
        WrongChannel,
        NotEnoughPlayer,
        NoPlayingGame,
        OfferCooldown,

        RunnerUpWin,
        MostOfferWin,

    }
    public class FlavorTextEntity : BaseEntity
    {
        public string Identifier { get; set; }
        public string Content { get; set; }
    }
}
