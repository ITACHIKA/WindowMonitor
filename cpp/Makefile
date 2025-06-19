TARGET = window_log

BUILD_DIR = BUILD

CXX = g++
CXXFLAGS = -std=c++17 -Wall -O0 -g

DLL_LDFLAGS = -shared -Wl,--out-implib,libmain.a
DLL_LDFLAGS_AFT = -lgdi32 -lgdiplus -lole32
DLL_EXPORT = -DBUILD_DLL

DLL_SRCS = bgInquiry.cpp

DLL_TARGET =$(BUILD_DIR)/$(TARGET).dll

DLL_OBJS = $(patsubst %.cpp,$(BUILD_DIR)/%.o,$(DLL_SRCS))

all:  $(BUILD_DIR) $(DLL_TARGET) $(EXE_TARGET) clear

$(BUILD_DIR):
	mkdir -p $(BUILD_DIR)

clear:
	rm -f  $(DLL_OBJS)

$(DLL_TARGET): $(DLL_OBJS)
	$(CXX) $(DLL_LDFLAGS) -o $@ $^ $(DLL_LDFLAGS_AFT)

$(BUILD_DIR)/%.o: %.cpp
ifeq ($(filter main.cpp,$<),main.cpp)
	$(CXX) $(CXXFLAGS) $(DLL_EXPORT) -c $< -o $@
else
	$(CXX) $(CXXFLAGS) -c $< -o $@
endif

clean:
	rm -rf $(BUILD_DIR)
