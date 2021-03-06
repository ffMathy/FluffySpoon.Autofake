﻿using FluffySpoon.Testing.Autofake.StructureMap;
using StructureMap;

namespace FluffySpoon.Testing.Autofake
{
	public static class RegistrationExtensions
    {
		public static void UseStructureMap(
			this Autofaker autofaker,
			IContainer container)
		{
			autofaker.Configure(
				new StructureMapInversionOfControlRegistration(container));
		}
    }
}
