using System;
using System.Collections.Generic;
using Mastercam.Database;
using Mastercam.Math;
using Mastercam.IO;
using Mastercam.Database.Types;
using Mastercam.GeometryUtility.Types;
using Mastercam.App.Types;
using Mastercam.GeometryUtility;
using Mastercam.Support;
using Mastercam.Curves;
using Mastercam.BasicGeometry;
using Mastercam.Database.Interop;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;
using System.Security.Claims;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace _rpGenCreases
{
    public class rpGenCreases : Mastercam.App.NetHook3App
    {
        public Mastercam.App.Types.MCamReturn rpGenCreasesRun(Mastercam.App.Types.MCamReturn notused)
        {
            var tempList1 = new List<int>(); // list of x+, y+ entities
            var tempList2 = new List<int>(); // list of x-, y- entitites
            var tempList3 = new List<int>(); // list of starting arcs
            var tempList4 = new List<int>(); // list of arc points
            var tempList5 = new List<Geometry>(); // list is broken arcs
            var tempList6 = new List<Geometry>(); // error list
            var tempList7 = new List<Geometry>(); // arcs to break
            var tempList8 = new List<int>(); // list of starting entities
            var tempList9 = new List<int>(); // list of all points
            var templist10 = new List<int>(); // temporarily holds geoID of chainEntities
            var templist80 = new List<int>();
            var templist81 = new List<int>();
            var upperCreaseID = new List<int>();
            var lowerCreaseID = new List<int>();
            var creaseX1 = 0.0;
            var creaseX2 = 0.0;
            var creaseY1 = 0.0;
            var creaseY2 = 0.0;
            var creaseX3 = 0.0;
            var creaseX4 = 0.0;
            var creaseY3 = 0.0;
            var creaseY4 = 0.0;

            bool CreateLine1()
            {
                bool result = false;
                Point3D crease1 = new Point3D(creaseX1, creaseY1, 0.0);
                Point3D crease3 = new Point3D(creaseX3, creaseY3, 0.0);
                LineGeometry Line1 = new LineGeometry(crease1, crease3);
                Line1.Selected = true;
                result = Line1.Commit();
                return result;
            } // Used for one line at end of crease
            bool CreateLine2()
            {
                bool result = false;
                Point3D crease2 = new Point3D(creaseX2, creaseY2, 0.0);
                Point3D crease4 = new Point3D(creaseX4, creaseY4, 0.0);
                LineGeometry Line2 = new LineGeometry(crease2, crease4);
                Line2.Selected = true;
                result = Line2.Commit();
                return result;
            } // Used for other line at other end of crease
            void offsetCreasechain()
            {
                //Unselects all Geometry
                SelectionManager.UnselectAllGeometry();
                //Turns off all visible levels
                var shown = LevelsManager.GetVisibleLevelNumbers();
                foreach (var level in shown)
                {
                    LevelsManager.SetLevelVisible(level, false);
                }
                LevelsManager.RefreshLevelsManager();
                //Sets level 101 to main level and visible
                LevelsManager.SetMainLevel(101);
                LevelsManager.SetLevelVisible(101, true);
                LevelsManager.RefreshLevelsManager();
                GraphicsManager.Repaint(true);//required

                //Selects all geometry on level 101
                var selectedCreaseChain = ChainManager.ChainAll(101);
                //Creates and names level 502 and level 503
                int createdUpperCrease = 502;
                int createdLowerCrease = 503;
                LevelsManager.SetLevelName(502, "Upper Created Crease Geo");
                LevelsManager.SetLevelName(503, "Lower Created Crease Geo");

                //edits each entity of all chains
                foreach (var chain in selectedCreaseChain)
                {
                    //offsets line of lower
                    var lowerChainCrease1 = chain.OffsetChain2D(OffsetSideType.Left, .040, OffsetRollCornerType.None, .5, false, .005, false);
                    var lowerChainCrease2 = chain.OffsetChain2D(OffsetSideType.Left, .065, OffsetRollCornerType.None, .5, false, .005, false);
                    var lowerChainCrease3 = chain.OffsetChain2D(OffsetSideType.Right, .040, OffsetRollCornerType.None, .5, false, .005, false);
                    var lowerChainCrease4 = chain.OffsetChain2D(OffsetSideType.Right, .065, OffsetRollCornerType.None, .5, false, .005, false);
                    //Colors and selects result geometry
                    var creaseResultGeometry = SearchManager.GetResultGeometry();
                    foreach (var entity in creaseResultGeometry)
                    {
                        lowerCreaseID.Add(entity.GetEntityID());
                        entity.Color = 11;
                        entity.Selected = true;
                        entity.Commit();
                    }
                    //Moves result geometry
                    GeometryManipulationManager.MoveSelectedGeometryToLevel(createdLowerCrease, true);
                    //Clears geometry in result
                    GraphicsManager.ClearColors(new GroupSelectionMask(true));

                    //offsets line of upper
                    var upperChainCrease1 = chain.OffsetChain2D(OffsetSideType.Left, .014, OffsetRollCornerType.None, .5, false, .005, false);
                    var upperChainCrease2 = chain.OffsetChain2D(OffsetSideType.Right, .014, OffsetRollCornerType.None, .5, false, .005, false);
                    //Colors and selects result geometry
                    var creaseResultGeometryNew = SearchManager.GetResultGeometry();
                    foreach (var entity in creaseResultGeometryNew)
                    {
                        upperCreaseID.Add(entity.GetEntityID());
                        entity.Color = 10;
                        entity.Selected = true;
                        entity.Commit();
                    }
                    //Moves result geometry
                    GeometryManipulationManager.MoveSelectedGeometryToLevel(createdUpperCrease, true);
                    //Clears geometry in result
                    GraphicsManager.ClearColors(new GroupSelectionMask(true));
                }
            } // Offsets creases
            void connectUpperLines()
            {
                var set = 0;
                foreach (var i in upperCreaseID)
                {
                    if (set == 0)
                    {
                        var creaseGeo = Geometry.RetrieveEntity(i);
                        var line = (LineGeometry)creaseGeo;
                        creaseX1 = line.EndPoint1.x;
                        creaseX2 = line.EndPoint2.x;
                        creaseY1 = line.EndPoint1.y;
                        creaseY2 = line.EndPoint2.y;
                        set = 1;
                    }
                    else
                    {
                        var creaseGeo2 = Geometry.RetrieveEntity(i);
                        var line2 = (LineGeometry)creaseGeo2;
                        creaseX3 = line2.EndPoint1.x;
                        creaseX4 = line2.EndPoint2.x;
                        creaseY3 = line2.EndPoint1.y;
                        creaseY4 = line2.EndPoint2.y;
                        CreateLine1();
                        CreateLine2();

                        var creaseResultGeometryNew = SearchManager.GetSelectedGeometry();
                        foreach (var entity in creaseResultGeometryNew)
                        {
                            entity.Color = 10;
                            entity.Selected = true;
                            entity.Commit();
                        }
                        //Moves result geometry
                        int createdUpperCrease = 502;
                        GeometryManipulationManager.MoveSelectedGeometryToLevel(createdUpperCrease, true);
                        //Clears geometry in result
                        GraphicsManager.ClearColors(new GroupSelectionMask(true));
                        //Deselects all
                        SelectionManager.UnselectAllGeometry();

                        set = 0;
                    }
                }
            } // Connects crease offsets for the upper
            void connectLowerLines()
            {
                var set = 0;
                foreach (var i in lowerCreaseID)
                {
                    if (set == 0)
                    {
                        var creaseGeo = Geometry.RetrieveEntity(i);
                        var line = (LineGeometry)creaseGeo;
                        creaseX1 = line.EndPoint1.x;
                        creaseX2 = line.EndPoint2.x;
                        creaseY1 = line.EndPoint1.y;
                        creaseY2 = line.EndPoint2.y;
                        set = 1;
                    }
                    else
                    {
                        var creaseGeo2 = Geometry.RetrieveEntity(i);
                        var line2 = (LineGeometry)creaseGeo2;
                        creaseX3 = line2.EndPoint1.x;
                        creaseX4 = line2.EndPoint2.x;
                        creaseY3 = line2.EndPoint1.y;
                        creaseY4 = line2.EndPoint2.y;
                        CreateLine1();
                        CreateLine2();

                        var creaseResultGeometryNew = SearchManager.GetSelectedGeometry();
                        foreach (var entity in creaseResultGeometryNew)
                        {
                            entity.Color = 11;
                            entity.Selected = true;
                            entity.Commit();
                        }
                        //Moves result geometry
                        int createdLowerCrease = 503;
                        GeometryManipulationManager.MoveSelectedGeometryToLevel(createdLowerCrease, true);
                        //Clears geometry in result
                        GraphicsManager.ClearColors(new GroupSelectionMask(true));
                        //Deselects all
                        SelectionManager.UnselectAllGeometry();

                        set = 0;
                    }
                }
            } // Connects crease offsets for the lower
            void DemoAlterLine()
            {
                //Unselects all Geometry
                SelectionManager.UnselectAllGeometry();
                //Turns off all visible levels
                var shown = LevelsManager.GetVisibleLevelNumbers();
                foreach (var level in shown)
                {
                    LevelsManager.SetLevelVisible(level, false);
                }
                LevelsManager.RefreshLevelsManager();
                //Sets level 101 to main level and visible
                LevelsManager.SetMainLevel(101);
                LevelsManager.SetLevelVisible(101, true);
                LevelsManager.RefreshLevelsManager();
                //GraphicsManager.Repaint(true);

                var geomask = new GeometryMask { Lines = true };
                var geoSel = new SelectionMask { };
                var geo = SearchManager.GetGeometry(geomask, geoSel, 101);
                if (geo != null)
                {
                    //Selects each line. Determines orientation and alters by 1 inch from both ends
                    foreach (var singleGeo in geo)
                    {
                        var line = (LineGeometry)singleGeo;
                        //If line is vertical
                        if (line.Data.Point1.x == line.Data.Point2.x)
                        {
                            if (line.Data.Point1.y >= line.Data.Point2.y)
                            {
                                line.Data.Point1.y += -0.07;
                                line.Data.Point2.y += +0.07;
                                line.Selected = false;
                            };
                            if (line.Data.Point1.y <= line.Data.Point2.y)
                            {
                                line.Data.Point1.y += +0.07;
                                line.Data.Point2.y += -0.07;
                                line.Selected = false;
                            };
                        };
                        //If line is horizontal
                        if (line.Data.Point1.y == line.Data.Point2.y)
                        {
                            if (line.Data.Point1.x >= line.Data.Point2.x)
                            {
                                line.Data.Point1.x += -0.07;
                                line.Data.Point2.x += +0.07;
                                line.Selected = false;
                            };
                            if (line.Data.Point1.x <= line.Data.Point2.x)
                            {
                                line.Data.Point1.x += +0.07;
                                line.Data.Point2.x += -0.07;
                                line.Selected = false;
                            };
                        };
                        //Stores result in Mastercam
                        line.Commit();
                    }
                    //Updates screen shown
                    //GraphicsManager.Repaint(true);
                }
            } // Shortens creases
            void deSelect()
            {
                var selectedGeo = SearchManager.GetGeometry();
                foreach (var entity in selectedGeo)
                {
                    entity.Retrieve();
                    entity.Selected = false;
                    entity.Commit();
                }
            }


            deSelect();
            DemoAlterLine();
            deSelect();
            offsetCreasechain();
            deSelect();
            connectUpperLines();
            deSelect();
            connectLowerLines();
            deSelect();

            GraphicsManager.Repaint(true);
            return MCamReturn.NoErrors;
        }
    }
}

        
    


            

                



            

        


    


/*

void offsetCutchain();
{

    var selectedChain = ChainManager.ChainAll();
    int createdUpperLevel = 500;
    int createdLowerLevel = 501;
    LevelsManager.SetLevelName(500, "Upper Created Geo");
    LevelsManager.SetLevelName(501, "Lower Created Geo");

    foreach (var chain in selectedChain)
    {


        var lowerChainLarge = chain.OffsetChain2D(OffsetSideType.Left, .0225, OffsetRollCornerType.None, .5, false, .005, false);
        var lowerLargeGeometry = ChainManager.GetGeometryInChain(lowerChainLarge);

        var lowerChainSmall = chain.OffsetChain2D(OffsetSideType.Right, .0025, OffsetRollCornerType.None, .5, false, .005, false);
        var lowerSmallGeometry = ChainManager.GetGeometryInChain(lowerChainSmall);

        var resultGeometry = SearchManager.GetResultGeometry();
        foreach (var entity in resultGeometry)
        {
            entity.Color = 11;
            entity.Selected = true;
            entity.Commit();
        }
        GeometryManipulationManager.MoveSelectedGeometryToLevel(createdLowerLevel, true);
        GraphicsManager.ClearColors(new GroupSelectionMask(true));

        var upperChainLarge = chain.OffsetChain2D(OffsetSideType.Left, .0025, OffsetRollCornerType.None, .5, false, .005, false);
        var upperLargeGeometry = ChainManager.GetGeometryInChain(upperChainLarge);

        var upperChainSmall = chain.OffsetChain2D(OffsetSideType.Right, .0385, OffsetRollCornerType.None, .5, false, .005, false);
        var upperSmallGeometry = ChainManager.GetGeometryInChain(upperChainSmall);

        var resultGeometryNew = SearchManager.GetResultGeometry();
        foreach (var entity in resultGeometryNew)
        {
            entity.Color = 10;
            entity.Selected = true;
            entity.Commit();
        }
        GeometryManipulationManager.MoveSelectedGeometryToLevel(createdUpperLevel, true);
        GraphicsManager.ClearColors(new GroupSelectionMask(true));

    }


}





// Working Offset Chain
/*

var selectedChain = ChainManager.GetOneChain("Select a Chain");

var offsetChain = selectedChain.OffsetChain2D(OffsetSideType.Left,
                                              .245,
                                              OffsetRollCornerType.None,
                                              .5,
                                              false,
                                              .005,
                                              false);

var offsetGeometry = ChainManager.GetGeometryInChain(offsetChain);

foreach (var entity in offsetGeometry)
{
    entity.Commit();
}

return MCamReturn.NoErrors;
*/





//Working Translate
/*
bool MoveLine() {
    bool result = false;
    //Mastercam.IO.SelectionManager.SelectAllGeometry();
    Mastercam.Math.Point3D pt1 = new Mastercam.Math.Point3D(0.0, 0.0, 0.0);
    Mastercam.Math.Point3D pt2 = new Mastercam.Math.Point3D(100.0, 0.0, 0.0);
    MCView Top = new MCView();
    Mastercam.GeometryUtility.GeometryManipulationManager.TranslateGeometry(pt1, pt2, Top , Top, false);
    return result;
}
MoveLine();
*/


// working form
/*
var m = new Form1();
m.Show();
*/

// working line creation
/*
bool CreateLine()
{
    bool result = false;

    Mastercam.Math.Point3D pt1 = new Mastercam.Math.Point3D(0.0, 0.0, 0.0);
    Mastercam.Math.Point3D pt2 = new Mastercam.Math.Point3D(100.0, 0.0, 0.0);
    Mastercam.Curves.LineGeometry Line1 = new Mastercam.Curves.LineGeometry(pt1, pt2);
    result = Line1.Commit();
    result = Line1.Validate(); // Not really needed here, if Commit was successful - we're good!
                               //Mastercam.IO.GraphicsManager.Repaint(True)

    return result;
}
CreateLine();
*/

//working popup message
//  System.Windows.Forms.MessageBox.Show("Jeremy can make pop up messages!");
//return Mastercam.App.Types.MCamReturn.NoErrors;
