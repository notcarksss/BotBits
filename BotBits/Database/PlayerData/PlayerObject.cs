﻿using System;
using PlayerIOClient;

namespace BotBits
{
    public class PlayerObject
    {
        private readonly DatabaseObject _databaseObject;

        public PlayerObject(DatabaseObject databaseObject)
        {
            this._databaseObject = databaseObject;
        }

        public bool ClubMember => this._databaseObject.Contains("gold_expire") &&
                                  this._databaseObject.GetDateTime("gold_expire") > DateTime.Now;

        public bool IsAdministrator => this._databaseObject.GetBool("isAdministrator", false);
    }
}