# Pixel Path 3D - 3D Creation Studio
**Student:** Ciobanu Andreea Simona, Team 4  
**Theme:** Innovative UX  
**Device:** Meta Quest 2  
**Module:** Hand Tracking  

# About the Project  
Pixel Path 3D is a VR application that allows users to create and animate 3D models using hand gestures.  
The application includes two main modules:  
  - **Modeling:** Create and edit 3D models using basic geometric shapes, tools, colors and hand-based object manipulation.  
  - **Animation:** Animate the 3D model using different layers for different parts of the object, keyframes and a simple timeline.  

# Project Documentation  

[View Pixel Path 3D Google Document](https://docs.google.com/document/d/1A2okfnWc2Dq9sr-CjnDbPX52-YoWj7oJlRQS3c47iVg/edit?usp=sharing)
The homework assignments are in separate tabs, as well as a Prompts tab with the main help AI provided, along with the link to the full AI conversation.

# Week 1 - Unity XR Setup 
**(19th to 26th October)**
Progress:
- Created the Unity 6.3 LTS project
- Configured Android and OpenXR for Meta Quest 2
- Installed XR Hands, XR Interaction Toolkit
- Enabled hand tracking and Meta Quest Support
- Imported and tested the Hands Interaction Demo
- Tested grabbing and moving 3D objects using simulated hands.

# Week 2 - VR Studio Grayboxing 
**(26th October to 2nd November)**
Progress:
- Created the VR studio graybox environment in Blender
- Modeled and imported the central platform and basic decorations
- Created prefabs for studio assets
- Added basic lighting
- Separate graybox scenes for Modeling and Animation workspaces
- Placeholders for panels

# Week 3 - VR Interaction and Modeling UI 
**(2nd to 9th November)**
[Week 3 Progress Video](https://drive.google.com/file/d/1OEKU9wQCgUB6bQ6NzdTVISoHyJ52vTJW/view?usp=sharing) 

Progress:
- Updated XR Interaction Toolkit and XR Hands samples
- Created custom fingerless gloves in Blender and imported them into Unity
- Integrated both gloves with the XR hand skeletons
- Tested hand tracking, Pinch, and Grab interactions
- Implemented project settings for the voxel modeling workspace
- Created the voxel grid and adjusted the central platform to fit its dimensions
- Designed and positioned interactive menu buttons on the hands
- Created prefabs for the Model, Edit, and Color hand menus
- Added icons and TextMeshPro labels to the buttons
- Organized the hand menu hierarchy for future tool functionality

# Week 4/5/6 - VR Modeling System Implementation 
**(9th to 30th November)**

Progress:
**Before Presentation: (9th to 23rd November)**

- Implemented Model Menu Tools: Brush and Eraser tools with continuous stroke, Select, Move, Rotate, Scale
- Implemented Edit Menu Tools: Duplicate, Delete, Group, Ungroup
- Implemented Color Menu Tools: Paint, Color Palette and Color Picker
- Added HSV color selection, custom palettes and palette pagination
- Added visual feedback for selected tools and interactive buttons
- Connected modeling tools to hand tracking gestures and interactive VR menus

**After Presentation: (23rd to 30th November)**
- Added Snap and Free placement modes
  - Snap Mode: Voxels are placed on the grid
  - Free Placement Mode: Voxels can be placed and moved freely
- Updated the grid visuals
- Added workspace rotation using the left hand
- Added continuous drawing for Brush in Free Placement mode
- Implemented Undo/Redo for the modeling tools
- Added Undo/Redo buttons on the right forearm
- Added keyboard shortcuts to debug easier

**Next Steps:**
- Need to create menus for save and load projects
- Implement Save/Load for models
- Add Import/Export functionality
- Start working on the Animation module
