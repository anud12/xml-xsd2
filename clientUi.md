## Todo
fill this file

## Layouting
Ui, use web `flex`, and `grid` on panel, and panel can have subpanels.

## Features
- Drag and drop


## 
Use OpenEXR file format for sprite pictures

Sprite mapping,
- have a frame sprite where colors are stored in 16 bits
  - red channel is the value of `x` coordinate in destination
  - green channel is the value of `y` coordinate in destination
  - blue channel with alpha channel is the pointer to the destination file.
- resulting image is the final image