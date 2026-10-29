# main.py
# Entry point for MGS PAD Editor

import sys
import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from editor_gui import main

if __name__ == "__main__":
    main()
