cmake_minimum_required(VERSION 3.20)

project(StatlynNativeConnector LANGUAGES CXX)

add_library(Statlyn.NativeConnector SHARED
  src/StatlynNativeConnector.cpp
)

target_compile_features(Statlyn.NativeConnector PRIVATE cxx_std_17)
target_compile_definitions(Statlyn.NativeConnector PRIVATE STATLYN_NATIVE_CONNECTOR_EXPORTS UNICODE _UNICODE)
target_include_directories(Statlyn.NativeConnector PUBLIC include)
target_link_libraries(Statlyn.NativeConnector PRIVATE Version)

set_target_properties(Statlyn.NativeConnector PROPERTIES
  OUTPUT_NAME "Statlyn.NativeConnector"
  RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
)
