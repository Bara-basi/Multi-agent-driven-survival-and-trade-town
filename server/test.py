import logging
logger = logging.getLogger(__name__)

class Test:
    def __init__(self):
        logger.info("Test class is created")

    def test_method(self):
        logger.info("Test method is called")


test = Test()
# test?.test_met
