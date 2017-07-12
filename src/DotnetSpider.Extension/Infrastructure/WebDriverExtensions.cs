﻿using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetSpider.Extension.Infrastructure
{
	public static class WebDriverExtensions
	{
		public static Image ElementSnapshot(this IWebElement element, Bitmap screenSnapshot)
		{
			Size size = new Size(Math.Min(element.Size.Width, screenSnapshot.Width),
				Math.Min(element.Size.Height, screenSnapshot.Height));
			Rectangle crop = new Rectangle(element.Location, size);
			return screenSnapshot.Clone(crop, screenSnapshot.PixelFormat);
		}
	}
}
