﻿using OpenStreetMap.API;
using OpenStreetMap.API.Models;

var overpassApi = new OverpassAPI();

var testArea = new Area
{
	LatNorth = 48.07083f,
	LatSouth = 48.06372f,
	LonEast = 20.63521f,
	LonWest = 20.62869f
};

await overpassApi.GetWaysOfArea(testArea, WayRank.SECONDARY | WayRank.PRIMARY);