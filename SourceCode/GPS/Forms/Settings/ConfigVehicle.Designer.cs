using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AgLibrary.Settings;
using AgLibrary.Logging;
using AgOpenGPS.Controls;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Core.Translations;
using AgOpenGPS.Properties;
using AgOpenGPS.ResourcesBrands;
using OpenTK.Graphics.OpenGL;

namespace AgOpenGPS
{
    public partial class FormConfig
    {
        #region Vehicle Save---------------------------------------------

        private void SaveDisplaySettings()
        {
            mf.isTextureOn = chkDisplayFloor.Checked;
            mf.isGridOn = chkDisplayGrid.Checked;
            mf.isSpeedoOn = chkDisplaySpeedo.Checked;
            mf.isSideGuideLines = chkDisplayExtraGuides.Checked;

            mf.isDrawPolygons = chkDisplayPolygons.Checked;
            mf.isKeyboardOn = chkDisplayKeyboard.Checked;

            mf.isBrightnessOn = chkDisplayBrightness.Checked;
            mf.isSvennArrowOn = chkSvennArrow.Checked;
            mf.isLogElevation = chkDisplayLogElevation.Checked;

            mf.isDirectionMarkers = chkDirectionMarkers.Checked;
            mf.isSectionlinesOn = chkSectionLines.Checked;
            mf.isLineSmooth = chkLineSmooth.Checked;
            mf.isHeadlandDistanceOn = chkboxHeadlandDist.Checked;

            //mf.timeToShowMenus = (int)nudMenusOnTime.Value;

            Properties.Settings.Default.setDisplay_isBrightnessOn = mf.isBrightnessOn;
            Properties.Settings.Default.setDisplay_isTextureOn = mf.isTextureOn;
            Properties.Settings.Default.setMenu_isGridOn = mf.isGridOn;

            Properties.Settings.Default.setDisplay_isSvennArrowOn = mf.isSvennArrowOn;
            Properties.Settings.Default.setMenu_isSpeedoOn = mf.isSpeedoOn;
            Properties.Settings.Default.setDisplay_isStartFullScreen = chkDisplayStartFullScreen.Checked;
            Properties.Settings.Default.setMenu_isSideGuideLines = mf.isSideGuideLines;

            Properties.Settings.Default.setMenu_isPureOn = mf.isPureDisplayOn;
            Properties.Settings.Default.setMenu_isLightbarOn = mf.isLightbarOn;
            Properties.Settings.Default.setDisplay_isKeyboardOn = mf.isKeyboardOn;
            Properties.Settings.Default.isHeadlandDistanceOn = mf.isHeadlandDistanceOn;
            Properties.Settings.Default.setDisplay_isLogElevation = mf.isLogElevation;

            Properties.Settings.Default.setMenu_isMetric = rbtnDisplayMetric.Checked;
            mf.isMetric = rbtnDisplayMetric.Checked;

            Properties.Settings.Default.setTool_isDirectionMarkers = mf.isDirectionMarkers;

            Properties.Settings.Default.setAS_numGuideLines = mf.ABLine.numGuideLines;
            Properties.Settings.Default.setDisplay_isSectionLinesOn = mf.isSectionlinesOn;
            Properties.Settings.Default.setDisplay_isLineSmooth = mf.isLineSmooth;
            Properties.Settings.Default.isHeadlandDistanceOn = mf.isHeadlandDistanceOn;

            Properties.Settings.Default.Save();
        }

        #endregion

        #region Antenna Enter/Leave
        private void tabVAntenna_Enter(object sender, EventArgs e)
        {
            nudAntennaHeight.Value = (int)(Properties.Settings.Default.setVehicle_antennaHeight * mf.m2InchOrCm);

            double antennaPivotSetting = Properties.Settings.Default.setVehicle_antennaPivot;

            if (mf.vehicle.VehicleConfig.Type == VehicleType.Articulated)
            {
                double pivotToFront = Math.Max(0, Properties.Settings.Default.setVehicle_articPivotToFront);
                double frontOffset = Properties.Settings.Default.setVehicle_antennaPivotFromFront;

                if (frontOffset <= 0)
                {
                    frontOffset = Math.Max(0, antennaPivotSetting - pivotToFront);
                    Properties.Settings.Default.setVehicle_antennaPivotFromFront = frontOffset;
                }

                antennaPivotSetting = frontOffset;
            }

            nudAntennaPivot.Value = (int)(antennaPivotSetting * mf.m2InchOrCm);

            //negative is to the right
            nudAntennaOffset.Value = (int)(Math.Abs(Properties.Settings.Default.setVehicle_antennaOffset) * mf.m2InchOrCm);

            rbtnAntennaLeft.Checked = false;
            rbtnAntennaRight.Checked = false;
            rbtnAntennaCenter.Checked = false;
            rbtnAntennaLeft.Checked = Properties.Settings.Default.setVehicle_antennaOffset > 0;
            rbtnAntennaRight.Checked = Properties.Settings.Default.setVehicle_antennaOffset < 0;
            rbtnAntennaCenter.Checked = Properties.Settings.Default.setVehicle_antennaOffset == 0;

            if (Properties.Settings.Default.setVehicle_vehicleType == 0)
                pboxAntenna.BackgroundImage = Properties.Resources.AntennaTractor;

            else if (Properties.Settings.Default.setVehicle_vehicleType == 1)
                pboxAntenna.BackgroundImage = Properties.Resources.AntennaHarvester;

            else if (Properties.Settings.Default.setVehicle_vehicleType == 2)
                pboxAntenna.BackgroundImage = Properties.Resources.AntennaArticulated;

            label98.Text = mf.unitsInCm;
            label99.Text = mf.unitsInCm;
            label100.Text = mf.unitsInCm;
        }

        private void tabVAntenna_Leave(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void rbtnAntennaLeft_Click(object sender, EventArgs e)
        {
            if (rbtnAntennaRight.Checked)
                mf.vehicle.VehicleConfig.AntennaOffset = (double)nudAntennaOffset.Value * -mf.inchOrCm2m;
            else if (rbtnAntennaLeft.Checked)
                mf.vehicle.VehicleConfig.AntennaOffset = (double)nudAntennaOffset.Value * mf.inchOrCm2m;
            else
            {
                mf.vehicle.VehicleConfig.AntennaOffset = 0;
                nudAntennaOffset.Value = 0;
            }

            Properties.Settings.Default.setVehicle_antennaOffset = mf.vehicle.VehicleConfig.AntennaOffset;
        }

        private void nudAntennaOffset_Click(object sender, EventArgs e)
        {
            if (((NudlessNumericUpDown)sender).ShowKeypad(this))
            {
                if ((double)nudAntennaOffset.Value == 0)
                {
                    rbtnAntennaLeft.Checked = false;
                    rbtnAntennaRight.Checked = false;
                    rbtnAntennaCenter.Checked = true;
                    mf.vehicle.VehicleConfig.AntennaOffset = 0;
                }
                else
                {
                    if (!rbtnAntennaLeft.Checked && !rbtnAntennaRight.Checked)
                        rbtnAntennaRight.Checked = true;

                    if (rbtnAntennaRight.Checked)
                        mf.vehicle.VehicleConfig.AntennaOffset = (double)nudAntennaOffset.Value * -mf.inchOrCm2m;
                    else
                        mf.vehicle.VehicleConfig.AntennaOffset = (double)nudAntennaOffset.Value * mf.inchOrCm2m;
                }

                Properties.Settings.Default.setVehicle_antennaOffset = mf.vehicle.VehicleConfig.AntennaOffset;
            }

            //rbtnAntennaLeft.Checked = false;
            //rbtnAntennaRight.Checked = false;
            //rbtnAntennaLeft.Checked = Properties.Settings.Default.setVehicle_antennaOffset > 0;
            //rbtnAntennaRight.Checked = Properties.Settings.Default.setVehicle_antennaOffset < 0;
        }

        private void nudAntennaPivot_Click(object sender, EventArgs e)
        {
            if (((NudlessNumericUpDown)sender).ShowKeypad(this))
            {
                double antennaValue = (double)nudAntennaPivot.Value * mf.inchOrCm2m;

                if (mf.vehicle.VehicleConfig.Type == VehicleType.Articulated)
                {
                    Properties.Settings.Default.setVehicle_antennaPivotFromFront = antennaValue;
                    double pivotToFront = Math.Max(0, Properties.Settings.Default.setVehicle_articPivotToFront);
                    double pivotDistance = pivotToFront + antennaValue;
                    Properties.Settings.Default.setVehicle_antennaPivot = pivotDistance;
                    mf.vehicle.VehicleConfig.AntennaPivotFromFrontAxle = antennaValue;
                    mf.vehicle.VehicleConfig.AntennaPivot = pivotDistance;
                }
                else
                {
                    Properties.Settings.Default.setVehicle_antennaPivot = antennaValue;
                    mf.vehicle.VehicleConfig.AntennaPivot = antennaValue;
                }
            }
        }

        private void nudAntennaHeight_Click(object sender, EventArgs e)
        {
            if (((NudlessNumericUpDown)sender).ShowKeypad(this))
            {
                Properties.Settings.Default.setVehicle_antennaHeight = (double)nudAntennaHeight.Value * mf.inchOrCm2m;
                mf.vehicle.VehicleConfig.AntennaHeight = Properties.Settings.Default.setVehicle_antennaHeight;
            }
        }

        #endregion

        #region Vehicle Dimensions

        private void tabVDimensions_Enter(object sender, EventArgs e)
        {
            double storedWheelbase = Math.Abs(Properties.Settings.Default.setVehicle_wheelbase);
            nudWheelbase.Value = (int)(storedWheelbase * mf.m2InchOrCm);

            nudVehicleTrack.Value = (int)(Math.Abs(Properties.Settings.Default.setVehicle_trackWidth) * mf.m2InchOrCm);

            nudTractorHitchLength.Value = (int)(Math.Abs(Properties.Settings.Default.setVehicle_hitchLength) * mf.m2InchOrCm);

            bool isArticulated = mf.vehicle.VehicleConfig.Type == VehicleType.Articulated;

            if (mf.vehicle.VehicleConfig.Type == VehicleType.Tractor)
            {
                pictureBox1.Image = Properties.Resources.RadiusWheelBase;
            }
            else if (mf.vehicle.VehicleConfig.Type == VehicleType.Harvester)
            {
                pictureBox1.Image = Properties.Resources.RadiusWheelBaseHarvester;
            }
            else if (isArticulated)
            {
                pictureBox1.Image = Properties.Resources.RadiusWheelBaseArticulated;
            }

            nudTractorHitchLength.Visible = rbtnTBT.Checked || rbtnTrailing.Checked;
            label94.Visible = rbtnTBT.Checked || rbtnTrailing.Checked;
            labelHitchLength.Visible = rbtnTBT.Checked || rbtnTrailing.Checked;
            HitchLengthBlindBox.Visible = rbtnFixedRear.Checked || rbtnFront.Checked;

            double pivotToFront = Math.Abs(Properties.Settings.Default.setVehicle_articPivotToFront);
            double pivotToRear = Math.Abs(Properties.Settings.Default.setVehicle_articPivotToRear);

            if (isArticulated && pivotToFront <= 0 && pivotToRear <= 0)
            {
                double halfWheelbase = storedWheelbase * 0.5;
                pivotToFront = halfWheelbase;
                pivotToRear = halfWheelbase;
                Properties.Settings.Default.setVehicle_articPivotToFront = pivotToFront;
                Properties.Settings.Default.setVehicle_articPivotToRear = pivotToRear;
                Properties.Settings.Default.setVehicle_wheelbase = pivotToFront + pivotToRear;
                mf.vehicle.VehicleConfig.PivotToFrontAxle = pivotToFront;
                mf.vehicle.VehicleConfig.PivotToRearAxle = pivotToRear;
                mf.vehicle.VehicleConfig.Wheelbase = pivotToFront + pivotToRear;
            }

            double articulatedWheelbase = pivotToFront + pivotToRear;

            if (isArticulated)
            {
                Properties.Settings.Default.setVehicle_wheelbase = articulatedWheelbase;
                mf.vehicle.VehicleConfig.PivotToFrontAxle = pivotToFront;
                mf.vehicle.VehicleConfig.PivotToRearAxle = pivotToRear;
                mf.vehicle.VehicleConfig.Wheelbase = articulatedWheelbase;
            }

            nudArticPivotToFront.Value = (int)(pivotToFront * mf.m2InchOrCm);
            nudArticPivotToRear.Value = (int)(pivotToRear * mf.m2InchOrCm);

            nudArticPivotToFront.Visible = isArticulated;
            labelPivotToFront.Visible = isArticulated;
            labelPivotFrontUnits.Visible = isArticulated;

            nudArticPivotToRear.Visible = isArticulated;
            labelPivotToRear.Visible = isArticulated;
            labelPivotRearUnits.Visible = isArticulated;

            nudWheelbase.Visible = !isArticulated;
            labelWheelBase2.Visible = !isArticulated;
            label97.Visible = !isArticulated;

            label94.Text = mf.unitsInCm;
            label95.Text = mf.unitsInCm;
            label97.Text = mf.unitsInCm;
            labelPivotFrontUnits.Text = mf.unitsInCm;
            labelPivotRearUnits.Text = mf.unitsInCm;
        }

        private void nudTractorHitchLength_Click(object sender, EventArgs e)
        {
            if (((NudlessNumericUpDown)sender).ShowKeypad(this))
            {
                mf.tool.hitchLength = (double)nudTractorHitchLength.Value * mf.inchOrCm2m;
                if (!Properties.Settings.Default.setTool_isToolFront)
                {
                    mf.tool.hitchLength *= -1;
                }
                Properties.Settings.Default.setVehicle_hitchLength = mf.tool.hitchLength;
            }
        }

        private void nudWheelbase_Click(object sender, EventArgs e)
        {
            if (((NudlessNumericUpDown)sender).ShowKeypad(this))
            {
                Properties.Settings.Default.setVehicle_wheelbase = (double)nudWheelbase.Value * mf.inchOrCm2m;
                mf.vehicle.VehicleConfig.Wheelbase = Properties.Settings.Default.setVehicle_wheelbase;
                Properties.Settings.Default.Save();
            }
        }

        private void nudArticPivotToFront_Click(object sender, EventArgs e)
        {
            if (((NudlessNumericUpDown)sender).ShowKeypad(this))
            {
                Properties.Settings.Default.setVehicle_articPivotToFront = (double)nudArticPivotToFront.Value * mf.inchOrCm2m;
                UpdateArticulatedWheelbaseSettings();
            }
        }

        private void nudArticPivotToRear_Click(object sender, EventArgs e)
        {
            if (((NudlessNumericUpDown)sender).ShowKeypad(this))
            {
                Properties.Settings.Default.setVehicle_articPivotToRear = (double)nudArticPivotToRear.Value * mf.inchOrCm2m;
                UpdateArticulatedWheelbaseSettings();
            }
        }

        private void UpdateArticulatedWheelbaseSettings()
        {
            double pivotToFront = Math.Max(0, Properties.Settings.Default.setVehicle_articPivotToFront);
            double pivotToRear = Math.Max(0, Properties.Settings.Default.setVehicle_articPivotToRear);
            double wheelbase = pivotToFront + pivotToRear;

            Properties.Settings.Default.setVehicle_wheelbase = wheelbase;
            mf.vehicle.VehicleConfig.PivotToFrontAxle = pivotToFront;
            mf.vehicle.VehicleConfig.PivotToRearAxle = pivotToRear;
            mf.vehicle.VehicleConfig.Wheelbase = wheelbase;

            double antennaFront = Properties.Settings.Default.setVehicle_antennaPivotFromFront;

            if (antennaFront <= 0)
            {
                double storedPivot = Properties.Settings.Default.setVehicle_antennaPivot;
                antennaFront = Math.Max(0, storedPivot - pivotToFront);
            }

            mf.vehicle.VehicleConfig.AntennaPivotFromFrontAxle = antennaFront;
            mf.vehicle.VehicleConfig.AntennaPivot = pivotToFront + antennaFront;
            Properties.Settings.Default.setVehicle_antennaPivotFromFront = antennaFront;
            Properties.Settings.Default.setVehicle_antennaPivot = mf.vehicle.VehicleConfig.AntennaPivot;

            Properties.Settings.Default.Save();
        }

        private void nudVehicleTrack_Click(object sender, EventArgs e)
        {
            if (((NudlessNumericUpDown)sender).ShowKeypad(this))
            {
                Properties.Settings.Default.setVehicle_trackWidth = (double)nudVehicleTrack.Value * mf.inchOrCm2m;
                mf.vehicle.VehicleConfig.TrackWidth = Properties.Settings.Default.setVehicle_trackWidth;
                mf.tram.halfWheelTrack = mf.vehicle.VehicleConfig.TrackWidth * 0.5;
                Properties.Settings.Default.Save();
            }
        }

        #endregion

        #region Vehicle Guidance

        private void tabVGuidance_Enter(object sender, EventArgs e)
        {
        }

        private void tabVGuidance_Leave(object sender, EventArgs e)
        {
        }
        #endregion

        #region VConfig Enter/Leave

        private void tabVConfig_Enter(object sender, EventArgs e)
        {
            configVehicleControl.Initialize(mf.vehicle.VehicleConfig);
        }

        private void tabVConfig_Leave(object sender, EventArgs e)
        {
            configVehicleControl.UpdateSettings();

            if (mf.vehicle.VehicleConfig.Type == VehicleType.Harvester)
            {
                if (mf.tool.hitchLength < 0) mf.tool.hitchLength *= -1;

                Settings.Default.setTool_isToolFront = true;
                Settings.Default.setTool_isToolTBT = false;
                Settings.Default.setTool_isToolTrailing = false;
                Settings.Default.setTool_isToolRearFixed = false;
            }

            switch (mf.vehicle.VehicleConfig.Type)
            {
                case VehicleType.Tractor:
                    mf.VehicleTextures.Tractor.SetBitmap(TractorBitmaps.GetBitmap(configVehicleControl.TractorBrand));
                    break;
                case VehicleType.Harvester:
                    mf.VehicleTextures.Harvester.SetBitmap(HarvesterBitmaps.GetBitmap(configVehicleControl.HarvesterBrand));
                    break;
                case VehicleType.Articulated:
                    mf.VehicleTextures.ArticulatedFront.SetBitmap(ArticulatedBitmaps.GetFrontBitmap(configVehicleControl.ArticulatedBrand));
                    mf.VehicleTextures.ArticulatedRear.SetBitmap(ArticulatedBitmaps.GetRearBitmap(configVehicleControl.ArticulatedBrand));
                    break;
            }

            Settings.Default.Save();
        }

        #endregion
    }
}

