# A-Language Software Development Kit (a-sdk)

A custom, bytecode-driven virtual machine and single-pass compiler toolchain written entirely from scratch in C# (.NET 10)... 

This environment compiles custom high-level language scripts into optimized bytecode instructions, which are then executed in a stack-based VM integrated directly with **Raylib 5.5** for hardware-accelerated 2D graphics!

---

## Integrated Master Toolchain CLI (`a.bat`)

The workspace comes bundled with a single command orchestrator to manage your building and running tasks:

* **Compile the SDK Target:** Bundles your compiler and runtime components into a single-file, trimmed standalone binary folder.
  ```powershell
  .\a build
  ```
* **Execute Script Documents:** Dynamically boots your bytecode engine and forwards a target script file parameter directly to the VM:
  ```powershell
  .\a run src/smth.a
  ```
* **Quick Fallback:** Running the command with no trailing script parameter will automatically default to executing `main.a` in your root workspace folder:
  ```powershell
  .\a run
  ```

---

## Hardware-Accelerated Graphics Sandbox Example (`main.a`)

This eh language binds namespace utilities (`use Standard.Game.Visual as v`) directly to written native code, supporting keyboard state polling, canvas clears, and shape renders out of the box (for now):

```rust
use Standard.Game.Visual as v
use Standard.Console as c

v.Init(800, 600, "🅰️ A-Language Graphics Sandbox")

let mut playerX = 375
let mut playerY = 275
let mut speed = 6

let bgColor = "0x1A1A2E"
let playerColor = "0xE94560"
let wallColor = "0x16C79A"

fn Update() {
    v.Clear(bgColor)

    if v.IsKeyDown("UP")    { playerY = playerY - speed }
    if v.IsKeyDown("DOWN")  { playerY = playerY + speed }
    if v.IsKeyDown("LEFT")  { playerX = playerX - speed }
    if v.IsKeyDown("RIGHT") { playerX = playerX + speed }

    if playerX < 10 { playerX = 10 }
    if playerX > 740 { playerX = 740 }
    if playerY < 10 { playerY = 10 }
    if playerY > 540 { playerY = 540 }
}

fn Render() {
    v.DrawRect(0, 0, 800, 10, wallColor)     
    v.DrawRect(0, 590, 800, 10, wallColor)   
    v.DrawRect(0, 0, 10, 600, wallColor)     
    v.DrawRect(790, 0, 10, 600, wallColor)   

    v.DrawRect(playerX, playerY, 50, 50, playerColor)
    v.Render()
}

fn Main() {
    c.Print("Preparing game loop...")
    while (v.IsOpen()) {
        Update()
        Render()
    }
    c.Print("Ending game loop!")
}

Main()
```
