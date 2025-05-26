# Paths
SOLUTION=../zpaq.sln
GUI_PROJECT=ZpaqGUI.csproj
GUI_DIR=.
CLI_SRC=../zpaq.cpp ../libzpaq.cpp
CLI_EXE=../zpaq.exe

# .NET build config
CONFIG=Release
FRAMEWORK=net7.0-windows

# Default target
all: cli gui copy-cli

# Build CLI (zpaq.exe)
cli:
	@echo Building ZPAQ CLI...
	cd .. && $(MAKE) -f Makefile

# Build GUI
.PHONY: gui
 gui:
	@echo Building ZPAQ GUI...
	dotnet build $(GUI_PROJECT) -c $(CONFIG)

# Copy CLI to GUI output
copy-cli:
	@echo Copying zpaq.exe to GUI output...
	@if exist $(CLI_EXE) copy $(CLI_EXE) bin\$(CONFIG)\$(FRAMEWORK)\zpaq.exe >nul

# Clean
.PHONY: clean
clean:
	dotnet clean $(GUI_PROJECT)
	@if exist bin rmdir /s /q bin
	@if exist obj rmdir /s /q obj

# Rebuild
.PHONY: rebuild
rebuild: clean all

# Run GUI
.PHONY: run
gui-run:
	@echo Running ZPAQ GUI...
	start bin\$(CONFIG)\$(FRAMEWORK)\ZpaqGUI.exe
