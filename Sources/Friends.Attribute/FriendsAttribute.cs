// <copyright file="FriendsAttribute.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Friends.Attribute
{
    using System;

    /// <summary>A FriendsAttribute is an attribute used to declare a method is a friend method.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FriendsAttribute : Attribute
    {
        private Type type;

        /// <summary>Initializes a new instance of the <see cref="FriendsAttribute" /> class.</summary>
        /// <param name="type">The friend type.</param>
        public FriendsAttribute(Type type)
        {
            this.type = type;
        }

        /// <summary>Gets the friend type.</summary>
        /// <value>The friend type.</value>
        public Type Type
        {
            get
            {
                return this.type;
            }
        }
    }
}
