/*=============================================================================
*
*	(C) Copyright 2010, Michael Carlisle (mike.carlisle@thecodeking.co.uk)
*
*   http://www.TheCodeKing.co.uk
*  
*	All rights reserved.
*	The code and information is provided "as-is" without waranty of any kind,
*	either expressed or implied.
*
*=============================================================================
*/
using System;
using System.Reflection;

namespace TheCodeKing.AutoBuilder.Interfaces
{
    internal interface IObjectParser
    {
        void ParseMembers<T>(Action<MemberInfo> callback);
        void ParseMembers(Type type, Action<MemberInfo> callback);
    }
}